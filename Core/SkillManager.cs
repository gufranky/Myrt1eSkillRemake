using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class SkillManager
{
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly SkillRegistry _registry;
    private readonly PerformanceMonitor _performance;
    private readonly Dictionary<int, PlayerSession> _sessions = new();

    public int AssignedPlayerCount => _sessions.Values.Count(session => session.Assignments.Count > 0);
    public int ActiveAssignmentCount => _sessions.Values.Sum(session => session.Assignments.Count);

    public IReadOnlyList<SkillDescriptor> GetAssignedSkills(CCSPlayerController player)
    {
        if (!_sessions.TryGetValue(player.Slot, out var session) || !session.Matches(player))
        {
            return Array.Empty<SkillDescriptor>();
        }

        return session.Assignments.Select(item => item.Skill.Descriptor).ToArray();
    }

    public SkillManager(
        Myrt1eSkillRemakePlugin plugin,
        SkillRegistry registry,
        PerformanceMonitor performance)
    {
        _plugin = plugin;
        _registry = registry;
        _performance = performance;
    }

    public void AssignAllPlayers(SkillPlan plan)
    {
        if (!plan.Enabled)
        {
            RevokeAll();
            return;
        }

        RevokeAll();
        var eligiblePlayers = Utilities.GetPlayers().Where(IsEligiblePlayer).ToArray();

        foreach (var player in eligiblePlayers)
        {
            AssignPlayer(player, eligiblePlayers, plan);
        }
    }

    public void RevokeAll(bool clearSessions = false)
    {
        foreach (var session in _sessions.Values.ToArray())
        {
            RevokeSession(session);
        }

        if (clearSessions)
        {
            _sessions.Clear();
        }
    }

    public void RemovePlayer(CCSPlayerController? player)
    {
        if (player is null || !_sessions.Remove(player.Slot, out var session))
        {
            return;
        }

        RevokeSession(session, player);
    }

    public bool TryActivate(CCSPlayerController? player)
    {
        if (player is null || !player.IsValid || !player.PawnIsAlive)
        {
            return false;
        }

        if (!_sessions.TryGetValue(player.Slot, out var session) || !session.Matches(player))
        {
            return false;
        }

        var assignment = session.Assignments.FirstOrDefault(item => item.Skill.Descriptor.Kind == SkillKind.Active);
        if (assignment is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        if (assignment.CooldownEndsAt > now)
        {
            var remaining = (assignment.CooldownEndsAt - now).TotalSeconds;
            PluginText.Center(player, $"技能冷却中：{remaining:F1} 秒");
            return false;
        }

        try
        {
            var context = CreateContext(player, assignment);
            assignment.Skill.OnActivated(context);
            assignment.CooldownEndsAt = now.AddSeconds(Math.Max(0, assignment.Skill.Descriptor.CooldownSeconds));
            return true;
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(
                exception,
                "Skill {SkillId} activation failed for slot {Slot}",
                assignment.Skill.Descriptor.Id,
                player.Slot);
            return false;
        }
    }

    public void Dispatch<THandler>(
        string operation,
        Action<THandler, SkillContext> callback)
        where THandler : class
    {
        _performance.Measure(operation, () =>
        {
            var snapshot = _sessions.Values
                .SelectMany(session => session.Assignments.Select(assignment => (session, assignment)))
                .Where(entry => entry.assignment.Skill is THandler)
                .ToArray();

            foreach (var (session, assignment) in snapshot)
            {
                var player = Utilities.GetPlayerFromSlot(session.Slot);
                if (player is null || !player.IsValid || !session.Matches(player))
                {
                    continue;
                }

                try
                {
                    var handler = (THandler)assignment.Skill;
                    callback(handler, CreateContext(player, assignment));
                }
                catch (Exception exception)
                {
                    _plugin.Logger.LogError(
                        exception,
                        "Skill {SkillId} failed while handling {Operation}",
                        assignment.Skill.Descriptor.Id,
                        operation);
                }
            }
        });
    }

    public void DispatchForPlayer<THandler>(
        CCSPlayerController? player,
        string operation,
        Action<THandler, SkillContext> callback)
        where THandler : class
    {
        if (player is null || !player.IsValid)
        {
            return;
        }

        if (!_sessions.TryGetValue(player.Slot, out var session) || !session.Matches(player))
        {
            return;
        }

        _performance.Measure(operation, () =>
        {
            foreach (var assignment in session.Assignments
                         .Where(item => item.Skill is THandler)
                         .ToArray())
            {
                try
                {
                    callback((THandler)assignment.Skill, CreateContext(player, assignment));
                }
                catch (Exception exception)
                {
                    _plugin.Logger.LogError(
                        exception,
                        "Skill {SkillId} failed while handling {Operation} for slot {Slot}",
                        assignment.Skill.Descriptor.Id,
                        operation,
                        player.Slot);
                }
            }
        });
    }

    private void AssignPlayer(
        CCSPlayerController player,
        IReadOnlyCollection<CCSPlayerController> eligiblePlayers,
        SkillPlan plan)
    {
        var session = GetOrCreateSession(player);
        var selected = new List<ISkill>();
        var requestedCount = Math.Clamp(plan.SlotsPerPlayer, 0, 8);

        if (plan.ForcedMode is ForcedSkillMode.ReplaceAll or ForcedSkillMode.EnsureIncluded)
        {
            foreach (var skillId in plan.ForcedSkillIds)
            {
                if (selected.Count >= requestedCount)
                {
                    break;
                }

                if (!_registry.TryGet(skillId, out var forcedSkill) || forcedSkill is null)
                {
                    _plugin.Logger.LogWarning("Forced skill {SkillId} is not registered", skillId);
                    continue;
                }

                if (!CanSelectSkill(forcedSkill, player, selected, eligiblePlayers, plan))
                {
                    _plugin.Logger.LogWarning(
                        "Forced skill {SkillId} is incompatible with the resolved round plan for slot {Slot}",
                        skillId,
                        player.Slot);
                    continue;
                }

                selected.Add(forcedSkill);
            }
        }

        if (plan.ForcedMode != ForcedSkillMode.ReplaceAll)
        {
            while (selected.Count < requestedCount)
            {
                var candidates = GetCandidates(player, session, selected, eligiblePlayers, plan);
                var skill = RaritySelector.Select(
                    candidates,
                    GetRarity,
                    GetRarityWeight,
                    GetWeight);

                if (skill is null)
                {
                    break;
                }

                selected.Add(skill);
            }
        }

        foreach (var skill in selected)
        {
            GrantSkill(session, player, skill);
        }
    }

    private void GrantSkill(PlayerSession session, CCSPlayerController player, ISkill skill)
    {
        var effects = new EffectScope(_plugin);
        var state = new SkillStateBag();
        var assignment = new SkillAssignment
        {
            Skill = skill,
            Effects = effects,
            State = state
        };

        try
        {
            skill.OnGranted(CreateContext(player, assignment));
            session.Assignments.Add(assignment);
            Remember(session, skill.Descriptor.Id);
        }
        catch (Exception exception)
        {
            effects.Dispose();
            state.Clear();
            _plugin.Logger.LogError(
                exception,
                "Skill {SkillId} grant failed for slot {Slot}",
                skill.Descriptor.Id,
                player.Slot);
        }
    }

    private IReadOnlyCollection<ISkill> GetCandidates(
        CCSPlayerController player,
        PlayerSession session,
        IReadOnlyCollection<ISkill> selected,
        IReadOnlyCollection<CCSPlayerController> eligiblePlayers,
        SkillPlan plan)
    {
        var selectedIds = selected
            .Select(skill => skill.Descriptor.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedTags = selected
            .SelectMany(skill => skill.Descriptor.ConflictTags)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var compatible = _registry.All
            .Where(skill => !selectedIds.Contains(skill.Descriptor.Id))
            .Where(skill => plan.ForcedMode != ForcedSkillMode.PoolOnly || plan.ForcedSkillIds.Contains(skill.Descriptor.Id, StringComparer.OrdinalIgnoreCase))
            .Where(skill => CanSelectSkill(skill, player, selected, eligiblePlayers, plan, usedTags))
            .ToArray();

        var preferred = compatible
            .Where(skill => !session.RecentSkills.Contains(skill.Descriptor.Id))
            .ToArray();

        // A small enabled pool must not deadlock merely because all skills are recent.
        return preferred.Length > 0 ? preferred : compatible;
    }

    private bool CanSelectSkill(
        ISkill skill,
        CCSPlayerController player,
        IReadOnlyCollection<ISkill> selected,
        IReadOnlyCollection<CCSPlayerController> eligiblePlayers,
        SkillPlan plan,
        IReadOnlySet<string>? usedTags = null)
    {
        if (!IsEnabled(skill) || GetWeight(skill) <= 0)
        {
            return false;
        }

        if (!CompatibilityResolver.IsSkillCompatible(skill.Descriptor, plan))
        {
            return false;
        }

        usedTags ??= selected
            .SelectMany(item => item.Descriptor.ConflictTags)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (skill.Descriptor.ConflictTags.Overlaps(usedTags))
        {
            return false;
        }

        if (skill.Descriptor.Kind == SkillKind.Active
            && selected.Count(item => item.Descriptor.Kind == SkillKind.Active) >= plan.MaxActiveSkillsPerPlayer)
        {
            return false;
        }

        return IsEligibleForPlayer(skill, player, eligiblePlayers)
            && IsBelowServerLimit(skill, selected);
    }

    private bool IsEligibleForPlayer(
        ISkill skill,
        CCSPlayerController player,
        IReadOnlyCollection<CCSPlayerController> eligiblePlayers)
    {
        var descriptor = skill.Descriptor;
        if (descriptor.OnlyTeam != CsTeam.None && player.Team != descriptor.OnlyTeam)
        {
            return false;
        }

        if (descriptor.RequiresTeammate && !eligiblePlayers.Any(other => other.Slot != player.Slot && other.Team == player.Team))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(descriptor.RequiredPermission))
        {
            if (player.IsBot || !AdminManager.PlayerHasPermissions(player, descriptor.RequiredPermission))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBelowServerLimit(ISkill skill, IReadOnlyCollection<ISkill> selected)
    {
        var limit = GetMaxPerServer(skill);
        if (limit < 0)
        {
            return true;
        }

        var id = skill.Descriptor.Id;
        var activeCount = _sessions.Values
            .SelectMany(session => session.Assignments)
            .Count(assignment => assignment.Skill.Descriptor.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        var selectedCount = selected.Count(item => item.Descriptor.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return activeCount + selectedCount < limit;
    }

    private PlayerSession GetOrCreateSession(CCSPlayerController player)
    {
        if (_sessions.TryGetValue(player.Slot, out var existing))
        {
            if (existing.Matches(player))
            {
                return existing;
            }

            RevokeSession(existing);
        }

        var created = new PlayerSession
        {
            Slot = player.Slot,
            ControllerIndex = player.Index,
            SteamId = player.SteamID
        };
        _sessions[player.Slot] = created;
        return created;
    }

    private void RevokeSession(PlayerSession session, CCSPlayerController? knownPlayer = null)
    {
        var player = knownPlayer ?? Utilities.GetPlayerFromSlot(session.Slot);

        foreach (var assignment in session.Assignments.AsEnumerable().Reverse().ToArray())
        {
            try
            {
                if (player is not null && player.IsValid && session.Matches(player))
                {
                    assignment.Skill.OnRevoked(CreateContext(player, assignment));
                }
            }
            catch (Exception exception)
            {
                _plugin.Logger.LogError(
                    exception,
                    "Failed to revoke skill {SkillId} from slot {Slot}",
                    assignment.Skill.Descriptor.Id,
                    session.Slot);
            }
            finally
            {
                assignment.Effects.Dispose();
                assignment.State.Clear();
            }
        }

        session.Assignments.Clear();
    }

    private SkillContext CreateContext(CCSPlayerController player, SkillAssignment assignment)
    {
        return new SkillContext(_plugin, player, assignment.Effects, assignment.State);
    }

    private void Remember(PlayerSession session, string skillId)
    {
        session.RecentSkills.Enqueue(skillId);
        var limit = Math.Max(0, _plugin.Config.RepeatBlockRounds);
        while (session.RecentSkills.Count > limit)
        {
            session.RecentSkills.Dequeue();
        }
    }

    private bool IsEnabled(ISkill skill)
    {
        return !TryGetOverride(skill, out var settings) || settings.Enabled;
    }

    private int GetWeight(ISkill skill)
    {
        return TryGetOverride(skill, out var settings) && settings.Weight.HasValue
            ? Math.Max(0, settings.Weight.Value)
            : Math.Max(0, skill.Descriptor.DefaultWeight);
    }

    private SkillRarity GetRarity(ISkill skill)
    {
        if (TryGetOverride(skill, out var settings)
            && Enum.TryParse<SkillRarity>(settings.Rarity, true, out var rarity))
        {
            return rarity;
        }

        return skill.Descriptor.Rarity;
    }

    private int GetRarityWeight(SkillRarity rarity)
    {
        var entry = _plugin.Config.RarityWeights.FirstOrDefault(pair =>
            pair.Key.Equals(rarity.ToString(), StringComparison.OrdinalIgnoreCase));
        return Math.Max(0, entry.Value);
    }

    private int GetMaxPerServer(ISkill skill)
    {
        return TryGetOverride(skill, out var settings) && settings.MaxPerServer.HasValue
            ? settings.MaxPerServer.Value
            : skill.Descriptor.MaxPerServer;
    }

    private bool TryGetOverride(ISkill skill, out SkillOverrideConfig settings)
    {
        var id = skill.Descriptor.Id;
        if (_plugin.Config.Skills.TryGetValue(id, out settings!))
        {
            return true;
        }

        settings = _plugin.Config.Skills
            .FirstOrDefault(pair => pair.Key.Equals(id, StringComparison.OrdinalIgnoreCase))
            .Value!;
        return settings is not null;
    }

    private static bool IsEligiblePlayer(CCSPlayerController player)
    {
        return player.IsValid
            && !player.IsHLTV
            && player.PawnIsAlive
            && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist;
    }
}
