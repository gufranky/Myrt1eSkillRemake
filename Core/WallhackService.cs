using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Owns one shared relay/glow pair per live player and exposes those pairs only
/// to Wallhack viewers through per-client CheckTransmit filtering.
/// </summary>
public sealed class WallhackService : IDisposable
{
    private sealed record GlowPair(uint RelayIndex, uint GlowIndex, CsTeam Team);
    private sealed record VisionGrant(bool Global, bool IncludeTeammates, HashSet<uint> Viewers);
    private readonly record struct ViewerPolicy(CsTeam Team, bool IncludeTeammates);

    private const int GlowsPerFrame = 4;
    private static readonly TimeSpan RemovedEntityBlockTime = TimeSpan.FromSeconds(2);

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly HashSet<uint> _viewers = new();
    private readonly Dictionary<string, VisionGrant> _eventGrants = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<uint, HashSet<uint>>> _targetedEventGrants = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<uint, GlowPair> _glows = new();
    private readonly Dictionary<uint, DateTime> _temporarilyBlocked = new();
    private readonly HashSet<int> _scheduledSlots = new();
    private int _buildGeneration;
    private bool _buildInProgress;
    private bool _disposed;

    public WallhackService(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
    }

    public void AddViewer(CCSPlayerController viewer)
    {
        if (_disposed || !viewer.IsValid || !_viewers.Add(viewer.Index))
        {
            return;
        }

        if (ActiveVisionSourceCount == 1)
        {
            StartBuild();
        }
    }

    public void RemoveViewer(CCSPlayerController? viewer)
    {
        if (viewer is null)
        {
            return;
        }

        _viewers.Remove(viewer.Index);
        if (ActiveVisionSourceCount == 0)
        {
            DestroyAll();
        }
    }

    public void SetSelectiveGrant(
        string sourceId,
        IEnumerable<CCSPlayerController> viewers,
        bool includeTeammates = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        var wasInactive = ActiveVisionSourceCount == 0;
        var indexes = viewers
            .Where(player => player is { IsValid: true })
            .Select(player => player.Index)
            .ToHashSet();
        _eventGrants[sourceId] = new VisionGrant(false, includeTeammates, indexes);
        if (wasInactive && ActiveVisionSourceCount > 0)
        {
            StartBuild();
        }
    }

    public void SetGlobalGrant(string sourceId, bool includeTeammates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        var wasInactive = ActiveVisionSourceCount == 0;
        _eventGrants[sourceId] = new VisionGrant(true, includeTeammates, new HashSet<uint>());
        if (wasInactive)
        {
            StartBuild();
        }
    }

    public void SetTargetedGrant(string sourceId, IEnumerable<(uint Viewer, uint Target)> grants)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(grants);
        var wasInactive = ActiveVisionSourceCount == 0;
        var targetsByViewer = new Dictionary<uint, HashSet<uint>>();
        foreach (var (viewer, target) in grants)
        {
            if (!targetsByViewer.TryGetValue(viewer, out var targets))
            {
                targets = new HashSet<uint>();
                targetsByViewer[viewer] = targets;
            }

            targets.Add(target);
        }

        if (targetsByViewer.Count == 0)
        {
            _targetedEventGrants.Remove(sourceId);
            if (ActiveVisionSourceCount == 0)
            {
                DestroyAll();
            }

            return;
        }

        _targetedEventGrants[sourceId] = targetsByViewer;
        if (wasInactive && ActiveVisionSourceCount > 0)
        {
            StartBuild();
        }
    }

    public void RemoveGrant(string sourceId)
    {
        _eventGrants.Remove(sourceId);
        _targetedEventGrants.Remove(sourceId);
        if (ActiveVisionSourceCount == 0)
        {
            DestroyAll();
        }
    }

    public void ScheduleTarget(CCSPlayerController? target)
    {
        if (_disposed || ActiveVisionSourceCount == 0 || target is null || !target.IsValid)
        {
            return;
        }

        if (!_scheduledSlots.Add(target.Slot))
        {
            return;
        }

        var slot = target.Slot;
        _plugin.AddTimer(0.10f, () =>
        {
            _scheduledSlots.Remove(slot);
            if (_disposed || ActiveVisionSourceCount == 0)
            {
                return;
            }

            var player = Utilities.GetPlayerFromSlot(slot);
            if (player is { IsValid: true, PawnIsAlive: true })
            {
                EnsureGlow(player);
            }
        });
    }

    public void RemoveTarget(CCSPlayerController? target)
    {
        if (target is null)
        {
            return;
        }

        RemoveGlow(target.Index);
    }

    public void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_glows.Count == 0 && _temporarilyBlocked.Count == 0)
        {
            return;
        }

        ExpireBlockedEntities();
        var glowSnapshot = BuildGlowSnapshot();
        BuildViewerPolicyMaps(out var policiesByController, out var policiesByPawn);
        BuildTargetedPolicyMaps(out var targetsByController, out var targetsByPawn);
        var globalPolicy = GetGlobalPolicy();

        foreach (var (info, observer) in infoList)
        {
            if (observer is not { IsValid: true })
            {
                continue;
            }

            var canSee = TryGetViewerPolicy(
                observer,
                globalPolicy,
                policiesByController,
                policiesByPawn,
                out var policy);
            var targeted = TryGetTargetedPolicy(observer, targetsByController, targetsByPawn);
            foreach (var glow in glowSnapshot)
            {
                var visibleByGeneralGrant = canSee
                    && (policy.IncludeTeammates || glow.Pair.Team != policy.Team);
                var visibleByTargetedGrant = targeted?.Contains(glow.TargetIndex) == true;
                if (glow.Showable && (visibleByGeneralGrant || visibleByTargetedGrant))
                {
                    continue;
                }

                info.TransmitEntities.Remove(glow.Pair.RelayIndex);
                info.TransmitEntities.Remove(glow.Pair.GlowIndex);
            }

            foreach (var entityIndex in _temporarilyBlocked.Keys)
            {
                info.TransmitEntities.Remove(entityIndex);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _viewers.Clear();
        _eventGrants.Clear();
        _targetedEventGrants.Clear();
        DestroyAll();
        _temporarilyBlocked.Clear();
        _scheduledSlots.Clear();
    }

    private void StartBuild()
    {
        if (_buildInProgress || _glows.Count > 0 || ActiveVisionSourceCount == 0)
        {
            return;
        }

        var targets = Utilities.GetPlayers()
            .Where(player => player.IsValid
                             && player.PawnIsAlive
                             && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            .Select(player => player.Index)
            .ToArray();

        _buildInProgress = true;
        var generation = ++_buildGeneration;
        SpawnChunk(targets, 0, generation);
    }

    private void SpawnChunk(uint[] targets, int start, int generation)
    {
        if (_disposed || generation != _buildGeneration || ActiveVisionSourceCount == 0)
        {
            _buildInProgress = false;
            return;
        }

        if (start >= targets.Length)
        {
            _buildInProgress = false;
            return;
        }

        var end = Math.Min(start + GlowsPerFrame, targets.Length);
        for (var index = start; index < end; index++)
        {
            var target = Utilities.GetPlayerFromIndex((int)targets[index]);
            if (target is { IsValid: true, PawnIsAlive: true })
            {
                EnsureGlow(target);
            }
        }

        Server.NextFrame(() => SpawnChunk(targets, end, generation));
    }

    private void EnsureGlow(CCSPlayerController target)
    {
        if (_glows.ContainsKey(target.Index))
        {
            return;
        }

        var pawn = target.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        var modelName = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState?.ModelName;
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return;
        }

        var relay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        var glow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (relay is null || glow is null)
        {
            SafeRemove(glow);
            SafeRemove(relay);
            return;
        }

        try
        {
            ClearNetworkedFlag(relay);
            relay.SetModel(modelName);
            relay.Spawnflags = 256u;
            relay.RenderMode = RenderMode_t.kRenderNone;
            relay.DispatchSpawn();

            ClearNetworkedFlag(glow);
            glow.SetModel(modelName);
            glow.Spawnflags = 256u;
            glow.Render = Color.FromArgb(1, 255, 255, 255);
            glow.DispatchSpawn();
            glow.Glow.GlowColorOverride = target.Team == CsTeam.Terrorist
                ? Color.FromArgb(255, 255, 165, 0)
                : Color.FromArgb(255, 173, 216, 230);
            glow.Glow.GlowRange = 5000;
            glow.Glow.GlowTeam = -1;
            glow.Glow.GlowType = 3;
            glow.Glow.GlowRangeMin = 100;

            relay.AcceptInput("FollowEntity", pawn, relay, "!activator");
            glow.AcceptInput("FollowEntity", relay, glow, "!activator");
            _glows[target.Index] = new GlowPair(relay.Index, glow.Index, target.Team);
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(exception, "Failed to create Wallhack glow for player {PlayerIndex}", target.Index);
            SafeRemove(glow);
            SafeRemove(relay);
        }
    }

    private List<(uint TargetIndex, GlowPair Pair, bool Showable)> BuildGlowSnapshot()
    {
        var snapshot = new List<(uint, GlowPair, bool)>(_glows.Count);
        foreach (var (targetIndex, pair) in _glows)
        {
            var target = Utilities.GetPlayerFromIndex((int)targetIndex);
            var pawn = target?.PlayerPawn.Value;
            var showable = target is { IsValid: true }
                           && pawn is { IsValid: true }
                           && pawn.Health > 0
                           && pawn.Render.A is not 102 and not 128
                           && IsEntityValid(pair.RelayIndex)
                           && IsEntityValid(pair.GlowIndex);
            snapshot.Add((targetIndex, pair, showable));
        }

        return snapshot;
    }

    private void BuildTargetedPolicyMaps(
        out Dictionary<uint, HashSet<uint>> byController,
        out Dictionary<nint, HashSet<uint>> byPawn)
    {
        byController = new Dictionary<uint, HashSet<uint>>();
        byPawn = new Dictionary<nint, HashSet<uint>>();
        foreach (var grant in _targetedEventGrants.Values)
        {
            foreach (var (viewerIndex, targets) in grant)
            {
                var viewer = Utilities.GetPlayerFromIndex((int)viewerIndex);
                var pawn = viewer?.PlayerPawn.Value;
                if (viewer is not { IsValid: true } || pawn is not { IsValid: true })
                {
                    continue;
                }

                MergeTargets(byController, viewerIndex, targets);
                MergeTargets(byPawn, pawn.Handle, targets);
            }
        }
    }

    private static HashSet<uint>? TryGetTargetedPolicy(
        CCSPlayerController observer,
        IReadOnlyDictionary<uint, HashSet<uint>> byController,
        IReadOnlyDictionary<nint, HashSet<uint>> byPawn)
    {
        if (byController.TryGetValue(observer.Index, out var targets))
        {
            return targets;
        }

        var observedHandle = observer.Pawn.Value?.ObserverServices?.ObserverTarget?.Value?.Handle ?? nint.Zero;
        return observedHandle != nint.Zero && byPawn.TryGetValue(observedHandle, out targets)
            ? targets
            : null;
    }

    private static void MergeTargets<TKey>(
        IDictionary<TKey, HashSet<uint>> policies,
        TKey key,
        IEnumerable<uint> targets)
        where TKey : notnull
    {
        if (!policies.TryGetValue(key, out var merged))
        {
            merged = new HashSet<uint>();
            policies[key] = merged;
        }

        merged.UnionWith(targets);
    }

    private void BuildViewerPolicyMaps(
        out Dictionary<uint, ViewerPolicy> byController,
        out Dictionary<nint, ViewerPolicy> byPawn)
    {
        byController = new Dictionary<uint, ViewerPolicy>();
        byPawn = new Dictionary<nint, ViewerPolicy>();
        foreach (var viewerIndex in _viewers.ToArray())
        {
            var viewer = Utilities.GetPlayerFromIndex((int)viewerIndex);
            var pawn = viewer?.PlayerPawn.Value;
            if (viewer is not { IsValid: true } || pawn is not { IsValid: true })
            {
                _viewers.Remove(viewerIndex);
                continue;
            }

            MergePolicy(byController, viewerIndex, new ViewerPolicy(viewer.Team, false));
            MergePolicy(byPawn, pawn.Handle, new ViewerPolicy(viewer.Team, false));
        }

        foreach (var grant in _eventGrants.Values.Where(grant => !grant.Global))
        {
            foreach (var viewerIndex in grant.Viewers.ToArray())
            {
                var viewer = Utilities.GetPlayerFromIndex((int)viewerIndex);
                var pawn = viewer?.PlayerPawn.Value;
                if (viewer is not { IsValid: true } || pawn is not { IsValid: true })
                {
                    grant.Viewers.Remove(viewerIndex);
                    continue;
                }

                var policy = new ViewerPolicy(viewer.Team, grant.IncludeTeammates);
                MergePolicy(byController, viewerIndex, policy);
                MergePolicy(byPawn, pawn.Handle, policy);
            }
        }
    }

    private static bool TryGetViewerPolicy(
        CCSPlayerController observer,
        ViewerPolicy? globalPolicy,
        IReadOnlyDictionary<uint, ViewerPolicy> policiesByController,
        IReadOnlyDictionary<nint, ViewerPolicy> policiesByPawn,
        out ViewerPolicy policy)
    {
        if (globalPolicy is { } everyone)
        {
            policy = everyone with { Team = observer.Team };
            return true;
        }

        if (policiesByController.TryGetValue(observer.Index, out policy))
        {
            return true;
        }

        var observedHandle = observer.Pawn.Value?.ObserverServices?.ObserverTarget?.Value?.Handle ?? nint.Zero;
        if (observedHandle != nint.Zero && policiesByPawn.TryGetValue(observedHandle, out policy))
        {
            return true;
        }

        policy = default;
        return false;
    }

    private ViewerPolicy? GetGlobalPolicy()
    {
        var globals = _eventGrants.Values.Where(grant => grant.Global).ToArray();
        return globals.Length == 0
            ? null
            : new ViewerPolicy(CsTeam.None, globals.Any(grant => grant.IncludeTeammates));
    }

    private static void MergePolicy<TKey>(
        IDictionary<TKey, ViewerPolicy> policies,
        TKey key,
        ViewerPolicy candidate)
        where TKey : notnull
    {
        if (policies.TryGetValue(key, out var current))
        {
            policies[key] = current with
            {
                Team = candidate.Team,
                IncludeTeammates = current.IncludeTeammates || candidate.IncludeTeammates
            };
            return;
        }

        policies[key] = candidate;
    }

    private void RemoveGlow(uint targetIndex)
    {
        if (!_glows.Remove(targetIndex, out var pair))
        {
            return;
        }

        SafeRemove(Utilities.GetEntityFromIndex<CDynamicProp>((int)pair.GlowIndex));
        SafeRemove(Utilities.GetEntityFromIndex<CDynamicProp>((int)pair.RelayIndex));
        var until = DateTime.UtcNow + RemovedEntityBlockTime;
        _temporarilyBlocked[pair.GlowIndex] = until;
        _temporarilyBlocked[pair.RelayIndex] = until;
    }

    private void DestroyAll()
    {
        _buildGeneration++;
        _buildInProgress = false;
        foreach (var targetIndex in _glows.Keys.ToArray())
        {
            RemoveGlow(targetIndex);
        }
    }

    private void ExpireBlockedEntities()
    {
        var now = DateTime.UtcNow;
        foreach (var (entityIndex, expiresAt) in _temporarilyBlocked.ToArray())
        {
            if (now >= expiresAt)
            {
                _temporarilyBlocked.Remove(entityIndex);
            }
        }
    }

    private static void ClearNetworkedFlag(CBaseEntity entity)
    {
        var networkedEntity = entity.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (networkedEntity is not null)
        {
            networkedEntity.Flags &= ~(uint)(1 << 2);
        }
    }

    private static bool IsEntityValid(uint entityIndex) =>
        Utilities.GetEntityFromIndex<CBaseEntity>((int)entityIndex) is { IsValid: true };

    private static void SafeRemove(CEntityInstance? entity)
    {
        if (entity is { IsValid: true })
        {
            entity.Remove();
        }
    }

    private int ActiveVisionSourceCount => _viewers.Count + _eventGrants.Count + _targetedEventGrants.Count;
}
