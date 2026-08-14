using Microsoft.Extensions.Logging;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class RoundEventManager
{
    private sealed class EventAssignment
    {
        public required IRoundEvent Event { get; init; }
        public required EffectScope Effects { get; init; }
        public required RoundPlan Plan { get; init; }
    }

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly EventRegistry _registry;
    private readonly PerformanceMonitor _performance;
    private readonly List<EventAssignment> _active = new();
    // Each item represents one complete round, including nested events.
    private readonly Queue<HashSet<string>> _eventHistory = new();
    private string? _forcedNextEventId;

    public RoundPlan? CurrentPlan { get; private set; }
    public IReadOnlyList<string> ActiveEventIds => _active.Select(item => item.Event.Descriptor.Id).ToArray();

    public RoundEventManager(
        Myrt1eSkillRemakePlugin plugin,
        EventRegistry registry,
        PerformanceMonitor performance)
    {
        _plugin = plugin;
        _registry = registry;
        _performance = performance;
    }

    public RoundPlan StartRound()
    {
        EndRound();
        var plan = ResolveRoundPlan();

        try
        {
            foreach (var descriptor in plan.Events)
            {
                if (!_registry.TryGet(descriptor.Id, out var roundEvent) || roundEvent is null)
                {
                    throw new InvalidOperationException($"Resolved event is not registered: {descriptor.Id}");
                }

                var effects = new EffectScope(_plugin);
                var assignment = new EventAssignment
                {
                    Event = roundEvent,
                    Effects = effects,
                    Plan = plan
                };

                try
                {
                    roundEvent.OnApplied(new RoundEventContext(_plugin, plan, effects));
                    _active.Add(assignment);
                }
                catch
                {
                    effects.Dispose();
                    throw;
                }
            }

            CurrentPlan = plan;
            _plugin.Logger.LogInformation(
                "Round plan resolved: events=[{Events}], skillsEnabled={Enabled}, slots={Slots}, forcedMode={ForcedMode}",
                string.Join(", ", plan.Events.Select(@event => @event.Id)),
                plan.Skills.Enabled,
                plan.Skills.SlotsPerPlayer,
                plan.Skills.ForcedMode);
            return plan;
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(exception, "Event application failed; rolling back the complete event set");
            EndRound();

            var fallback = ResolveNormalPlan();
            var normal = GetNormalEvent();
            var effects = new EffectScope(_plugin);
            try
            {
                normal.OnApplied(new RoundEventContext(_plugin, fallback, effects));
                _active.Add(new EventAssignment
                {
                    Event = normal,
                    Effects = effects,
                    Plan = fallback
                });
            }
            catch (Exception fallbackException)
            {
                effects.Dispose();
                _plugin.Logger.LogError(fallbackException, "NormalRound fallback application also failed");
            }

            CurrentPlan = fallback;
            return fallback;
        }
    }

    public bool ForceNextEvent(string eventId)
    {
        if (!_registry.TryGet(eventId, out var roundEvent) || roundEvent is null)
        {
            return false;
        }

        _forcedNextEventId = roundEvent.Descriptor.Id;
        return true;
    }

    public void EndRound()
    {
        foreach (var assignment in _active.AsEnumerable().Reverse().ToArray())
        {
            try
            {
                assignment.Event.OnRemoved(new RoundEventContext(_plugin, assignment.Plan, assignment.Effects));
            }
            catch (Exception exception)
            {
                _plugin.Logger.LogError(
                    exception,
                    "Failed to remove event {EventId}",
                    assignment.Event.Descriptor.Id);
            }
            finally
            {
                assignment.Effects.Dispose();
            }
        }

        _active.Clear();
        CurrentPlan = null;
    }

    public void Dispatch<THandler>(
        string operation,
        Action<THandler, RoundEventContext> callback)
        where THandler : class
    {
        _performance.Measure(operation, () =>
        {
            foreach (var assignment in _active.ToArray())
            {
                if (assignment.Event is not THandler handler)
                {
                    continue;
                }

                try
                {
                    callback(handler, new RoundEventContext(_plugin, assignment.Plan, assignment.Effects));
                }
                catch (Exception exception)
                {
                    _plugin.Logger.LogError(
                        exception,
                        "Event {EventId} failed while handling {Operation}",
                        assignment.Event.Descriptor.Id,
                        operation);
                }
            }
        });
    }

    private RoundPlan ResolveRoundPlan()
    {
        if (!_plugin.Config.EventsEnabled)
        {
            return ResolveNormalPlan();
        }

        IRoundEvent root;
        if (!string.IsNullOrWhiteSpace(_forcedNextEventId)
            && _registry.TryGet(_forcedNextEventId, out var forced)
            && forced is not null)
        {
            root = forced;
            _forcedNextEventId = null;
        }
        else
        {
            _forcedNextEventId = null;
            var rootCandidates = _registry.All
            .Where(IsEnabled)
            .Where(@event => GetWeight(@event) > 0)
            .Where(@event => !WasUsedRecently(@event.Descriptor.Id))
            .ToArray();

            if (rootCandidates.Length == 0)
            {
                rootCandidates = _registry.All
                    .Where(IsEnabled)
                    .Where(@event => GetWeight(@event) > 0)
                    .ToArray();
            }

            root = WeightedSelector.Select(rootCandidates, GetWeight) ?? GetNormalEvent();
        }

        var selected = new List<IRoundEvent> { root };
        var childCount = Math.Max(0, root.Descriptor.CompositeChildCount);
        var maxEvents = Math.Clamp(_plugin.Config.MaxEventsPerRound, 1, 8);
        childCount = Math.Min(childCount, maxEvents - 1);

        for (var index = 0; index < childCount; index++)
        {
            var candidates = _registry.All
                .Where(IsEnabled)
                .Where(@event => @event.Descriptor.CanBeNested)
                .Where(@event => @event.Descriptor.CompositeChildCount == 0)
                .Where(@event => GetWeight(@event) > 0)
                .Where(@event => !WasUsedRecently(@event.Descriptor.Id))
                .Where(@event => CompatibilityResolver.CanCombine(selected, @event))
                .ToArray();

            // Do not block a composite event when every compatible child is
            // in the history window; the root event must still be playable.
            if (candidates.Length == 0)
            {
                candidates = _registry.All
                    .Where(IsEnabled)
                    .Where(@event => @event.Descriptor.CanBeNested)
                    .Where(@event => @event.Descriptor.CompositeChildCount == 0)
                    .Where(@event => GetWeight(@event) > 0)
                    .Where(@event => CompatibilityResolver.CanCombine(selected, @event))
                    .ToArray();
            }

            var child = WeightedSelector.Select(candidates, GetWeight);
            if (child is null)
            {
                _plugin.Logger.LogWarning(
                    "Composite event {EventId} requested {Requested} children but only {Resolved} compatible children were resolved",
                    root.Descriptor.Id,
                    childCount,
                    selected.Count - 1);
                break;
            }

            selected.Add(child);
        }

        RememberEvents(selected);
        return BuildPlan(selected);
    }

    private RoundPlan ResolveNormalPlan()
    {
        return BuildPlan(new[] { GetNormalEvent() });
    }

    private RoundPlan BuildPlan(IReadOnlyCollection<IRoundEvent> selected)
    {
        var builder = new RoundPlanBuilder(_plugin.Config);
        builder.SetActiveEvents(selected);

        foreach (var roundEvent in selected)
        {
            roundEvent.Contribute(builder);
        }

        return builder.Build();
    }

    private IRoundEvent GetNormalEvent()
    {
        if (_registry.TryGet("NormalRound", out var normal) && normal is not null)
        {
            return normal;
        }

        throw new InvalidOperationException("NormalRound event is not registered.");
    }

    private bool IsEnabled(IRoundEvent roundEvent)
    {
        return !TryGetOverride(roundEvent, out var settings) || settings.Enabled;
    }

    private int GetWeight(IRoundEvent roundEvent)
    {
        return TryGetOverride(roundEvent, out var settings) && settings.Weight.HasValue
            ? Math.Max(0, settings.Weight.Value)
            : Math.Max(0, roundEvent.Descriptor.DefaultWeight);
    }

    private bool TryGetOverride(IRoundEvent roundEvent, out EventOverrideConfig settings)
    {
        var id = roundEvent.Descriptor.Id;
        if (_plugin.Config.Events.TryGetValue(id, out settings!))
        {
            return true;
        }

        settings = _plugin.Config.Events
            .FirstOrDefault(pair => pair.Key.Equals(id, StringComparison.OrdinalIgnoreCase))
            .Value!;
        return settings is not null;
    }

    private bool WasUsedRecently(string eventId) =>
        _eventHistory.Any(round => round.Contains(eventId));

    private void RememberEvents(IEnumerable<IRoundEvent> events)
    {
        _eventHistory.Enqueue(events
            .Select(@event => @event.Descriptor.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase));
        var limit = Math.Max(0, _plugin.Config.EventRepeatBlockRounds);
        while (_eventHistory.Count > limit)
        {
            _eventHistory.Dequeue();
        }
    }
}
