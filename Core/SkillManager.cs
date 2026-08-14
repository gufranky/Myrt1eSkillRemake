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
    private readonly Dictionary<string, HashSet<string>> _choiceReservations =
        new(StringComparer.Ordinal);
    private SkillPlan? _currentPlan;

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

    public IReadOnlyList<SkillDescriptor> GetActiveSkills(CCSPlayerController player)
    {
        if (!_sessions.TryGetValue(player.Slot, out var session) || !session.Matches(player))
        {
            return Array.Empty<SkillDescriptor>();
        }

        return session.Assignments
            .Where(item => item.Skill.Descriptor.Kind == SkillKind.Active)
            .Select(item => item.Skill.Descriptor)
            .ToArray();
    }

    public SkillDescriptor? TryGetDescriptor(string skillId) =>
        _registry.TryGet(skillId, out var skill) ? skill?.Descriptor : null;

    public IReadOnlyList<SkillDescriptor> InheritSkillsFromPlayer(
        CCSPlayerController inheritor,
        CCSPlayerController deceased,
        string sourceSkillId,
        int maximumSkillCount,
        out int totalSkillCount)
    {
        totalSkillCount = 0;
        if (!inheritor.IsValid
            || !inheritor.PawnIsAlive
            || !deceased.IsValid
            || inheritor.Index == deceased.Index
            || maximumSkillCount <= 0
            || !_sessions.TryGetValue(inheritor.Slot, out var inheritorSession)
            || !inheritorSession.Matches(inheritor)
            || !inheritorSession.Assignments.Any(assignment =>
                assignment.Skill.Descriptor.Id.Equals(sourceSkillId, StringComparison.OrdinalIgnoreCase))
            || !_sessions.TryGetValue(deceased.Slot, out var deceasedSession)
            || !deceasedSession.Matches(deceased))
        {
            return Array.Empty<SkillDescriptor>();
        }

        totalSkillCount = inheritorSession.Assignments.Count;
        if (totalSkillCount >= maximumSkillCount)
        {
            return Array.Empty<SkillDescriptor>();
        }

        var inherited = new List<SkillDescriptor>();
        var existingIds = inheritorSession.Assignments
            .Select(assignment => assignment.Skill.Descriptor.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedTags = inheritorSession.Assignments
            .SelectMany(assignment => assignment.Skill.Descriptor.ConflictTags)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasActiveSkill = inheritorSession.Assignments.Any(assignment =>
            assignment.Skill.Descriptor.Kind == SkillKind.Active);
        var eligiblePlayers = Utilities.GetPlayers().Where(IsEligiblePlayer).ToArray();
        var candidates = deceasedSession.Assignments
            .Select(assignment => assignment.Skill)
            .ToArray();

        foreach (var skill in candidates)
        {
            if (inheritorSession.Assignments.Count >= maximumSkillCount)
            {
                break;
            }

            var descriptor = skill.Descriptor;
            if (existingIds.Contains(descriptor.Id)
                || descriptor.ConflictTags.Overlaps(usedTags)
                || (descriptor.Kind == SkillKind.Active && hasActiveSkill)
                || !CanGrantRuntimeSkill(
                    skill,
                    inheritor,
                    eligiblePlayers,
                    ignoreServerLimit: true))
            {
                continue;
            }

            if (!GrantSkill(inheritorSession, inheritor, skill))
            {
                continue;
            }

            inherited.Add(descriptor);
            existingIds.Add(descriptor.Id);
            usedTags.UnionWith(descriptor.ConflictTags);
            hasActiveSkill |= descriptor.Kind == SkillKind.Active;
        }

        totalSkillCount = inheritorSession.Assignments.Count;
        return inherited;
    }

    public bool TryDeactivatePlayerSkills(
        CCSPlayerController caster,
        CCSPlayerController target,
        string sourceSkillId,
        out IReadOnlyList<SkillDescriptor> disabledSkills,
        out string error)
    {
        disabledSkills = Array.Empty<SkillDescriptor>();
        error = string.Empty;
        if (!caster.IsValid || !caster.PawnIsAlive || !target.IsValid || !target.PawnIsAlive)
        {
            error = "施法者或目标已经失效。";
            return false;
        }

        if (caster.Index == target.Index || caster.Team == target.Team)
        {
            error = "只能禁用一名存活敌人的技能。";
            return false;
        }

        if (!_sessions.TryGetValue(target.Slot, out var targetSession)
            || !targetSession.Matches(target)
            || targetSession.Assignments.Count == 0)
        {
            error = $"{target.PlayerName} 当前没有可以禁用的技能。";
            return false;
        }

        if (!_sessions.TryGetValue(caster.Slot, out var casterSession)
            || !casterSession.Matches(caster)
            || !casterSession.Assignments.Any(assignment =>
                assignment.Skill.Descriptor.Id.Equals(sourceSkillId, StringComparison.OrdinalIgnoreCase)))
        {
            error = "技能终止能力已经失效。";
            return false;
        }

        disabledSkills = targetSession.Assignments
            .Select(assignment => assignment.Skill.Descriptor)
            .ToArray();
        RevokeSession(targetSession, target);

        var sourceAssignment = casterSession.Assignments.First(assignment =>
            assignment.Skill.Descriptor.Id.Equals(sourceSkillId, StringComparison.OrdinalIgnoreCase));
        RevokeAssignment(casterSession, sourceAssignment, caster);
        return true;
    }

    public IReadOnlyList<SkillDescriptor> DrawSkillChoices(
        CCSPlayerController player,
        int count,
        string reservationOwner,
        params string[] excludedSkillIds)
    {
        if (!player.IsValid
            || !player.PawnIsAlive
            || count <= 0
            || string.IsNullOrWhiteSpace(reservationOwner))
        {
            return Array.Empty<SkillDescriptor>();
        }

        ReleaseSkillChoiceReservations(reservationOwner);

        var excluded = excludedSkillIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (_sessions.TryGetValue(player.Slot, out var currentSession) && currentSession.Matches(player))
        {
            excluded.UnionWith(currentSession.Assignments.Select(item => item.Skill.Descriptor.Id));
        }

        var eligiblePlayers = Utilities.GetPlayers().Where(IsEligiblePlayer).ToArray();
        var candidates = _registry.All
            .Where(skill => !excluded.Contains(skill.Descriptor.Id))
            .Where(skill => CanGrantRuntimeSkill(skill, player, eligiblePlayers))
            .ToList();
        if (_sessions.TryGetValue(player.Slot, out var session) && session.Matches(player))
        {
            var preferred = candidates
                .Where(skill => !session.HasRecentSkill(skill.Descriptor.Id))
                .ToList();
            if (preferred.Count >= Math.Min(count, candidates.Count))
            {
                candidates = preferred;
            }
        }

        var selected = new List<ISkill>();
        while (selected.Count < count && candidates.Count > 0)
        {
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
            candidates.Remove(skill);
        }

        // Only a complete menu owns reservations. A failed three-choice draw
        // must not consume scarce slots without presenting them to the player.
        if (selected.Count == count)
        {
            _choiceReservations[reservationOwner] = selected
                .Where(skill => GetMaxPerServer(skill) >= 0)
                .Select(skill => skill.Descriptor.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return selected.Select(skill => skill.Descriptor).ToArray();
    }

    public void ReleaseSkillChoiceReservations(string reservationOwner)
    {
        if (!string.IsNullOrWhiteSpace(reservationOwner))
        {
            _choiceReservations.Remove(reservationOwner);
        }
    }

    public static bool HasServerCapacity(
        int limit,
        int activeCount,
        int selectedCount,
        int reservedCount) =>
        limit < 0 || activeCount + selectedCount + reservedCount < limit;

    public bool TryReplaceWithSkill(
        CCSPlayerController player,
        string skillId,
        out SkillDescriptor? grantedSkill,
        out string error)
    {
        grantedSkill = null;
        error = string.Empty;
        if (!player.IsValid || !player.PawnIsAlive)
        {
            error = "玩家已经失效。";
            return false;
        }

        if (!_registry.TryGet(skillId, out var skill) || skill is null)
        {
            error = "所选技能不存在。";
            return false;
        }

        var eligiblePlayers = Utilities.GetPlayers().Where(IsEligiblePlayer).ToArray();
        // DrawSkillChoices already applied the server-cap filter before this
        // skill was shown. Confirmation must honor that offered choice even if
        // another acquisition fills the cap while the menu remains open.
        if (!CanGrantRuntimeSkill(
                skill,
                player,
                eligiblePlayers,
                ignoreServerLimit: true))
        {
            error = $"技能“{skill.Descriptor.DisplayName}”当前不可用。";
            return false;
        }

        var session = GetOrCreateSession(player);
        var previousSkills = session.Assignments.Select(item => item.Skill).ToArray();
        RevokeSession(session, player);
        if (!GrantSkill(session, player, skill))
        {
            foreach (var previousSkill in previousSkills)
            {
                GrantSkill(session, player, previousSkill);
            }

            error = $"应用技能“{skill.Descriptor.DisplayName}”失败。";
            return false;
        }

        Remember(session, skill.Descriptor.Id);
        grantedSkill = skill.Descriptor;
        return true;
    }

    public bool TryReplaceSkillsFromPlayer(
        CCSPlayerController copier,
        CCSPlayerController target,
        out IReadOnlyList<SkillDescriptor> copiedSkills,
        out string error)
    {
        copiedSkills = Array.Empty<SkillDescriptor>();
        error = string.Empty;
        if (!copier.IsValid || !copier.PawnIsAlive || !target.IsValid || !target.PawnIsAlive)
        {
            error = "施法者或目标已经失效。";
            return false;
        }

        if (copier.Index == target.Index || copier.Team == target.Team)
        {
            error = "只能复制一名存活敌人的技能。";
            return false;
        }

        if (!_sessions.TryGetValue(target.Slot, out var targetSession)
            || !targetSession.Matches(target)
            || targetSession.Assignments.Count == 0)
        {
            error = $"{target.PlayerName} 当前没有可复制的技能。";
            return false;
        }

        var skillsToCopy = targetSession.Assignments.Select(item => item.Skill).ToArray();
        var eligiblePlayers = Utilities.GetPlayers().Where(IsEligiblePlayer).ToArray();
        var incompatible = skillsToCopy.FirstOrDefault(skill =>
            !IsEligibleForPlayer(skill, copier, eligiblePlayers)
            || (_currentPlan is not null
                && !CompatibilityResolver.IsSkillCompatible(skill.Descriptor, _currentPlan)));
        if (incompatible is not null)
        {
            error = $"目标的技能“{incompatible.Descriptor.DisplayName}”不适用于你或当前事件。";
            return false;
        }

        var copierSession = GetOrCreateSession(copier);
        var previousSkills = copierSession.Assignments.Select(item => item.Skill).ToArray();
        RevokeSession(copierSession, copier);

        foreach (var skill in skillsToCopy)
        {
            if (GrantSkill(copierSession, copier, skill))
            {
                continue;
            }

            RevokeSession(copierSession, copier);
            foreach (var previousSkill in previousSkills)
            {
                GrantSkill(copierSession, copier, previousSkill);
            }

            error = $"复制技能“{skill.Descriptor.DisplayName}”时应用失败。";
            return false;
        }

        copiedSkills = skillsToCopy.Select(skill => skill.Descriptor).ToArray();
        return true;
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
        _currentPlan = plan;
        var eligiblePlayers = Utilities.GetPlayers().Where(IsEligiblePlayer).ToArray();
        foreach (var player in eligiblePlayers)
        {
            GetOrCreateSession(player).BeginSkillRound(_plugin.Config.RepeatBlockRounds);
        }

        if (!plan.Enabled)
        {
            RevokeAll();
            return;
        }

        RevokeAll();
        var recipients = plan.AssignmentMode == SkillAssignmentMode.OneRandomPlayerPerTeam
            ? SelectOneRandomPlayerPerTeam(eligiblePlayers)
            : eligiblePlayers;

        foreach (var player in recipients)
        {
            AssignPlayer(player, eligiblePlayers, plan);
        }

        if (plan.AssignmentMode == SkillAssignmentMode.OneRandomPlayerPerTeam)
        {
            foreach (var champion in recipients)
            {
                PluginText.ChatAll(
                    $"[我是达人] ⭐ {champion.PlayerName} 成为 {TeamName(champion.Team)} 达人，获得 {GetAssignedSkills(champion).Count} 个技能！");
                PluginText.Center(champion, "⭐ 你是本队达人！");
            }
        }
    }

    public void RevokeAll(bool clearSessions = false)
    {
        foreach (var session in _sessions.Values.ToArray())
        {
            RevokeSession(session);
        }

        _choiceReservations.Clear();

        if (clearSessions)
        {
            _sessions.Clear();
            _currentPlan = null;
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

    public bool TryActivate(CCSPlayerController? player, string? skillId = null)
    {
        if (player is null || !player.IsValid || !player.PawnIsAlive)
        {
            return false;
        }

        if (!_sessions.TryGetValue(player.Slot, out var session) || !session.Matches(player))
        {
            return false;
        }

        var assignment = session.Assignments.FirstOrDefault(item =>
            item.Skill.Descriptor.Kind == SkillKind.Active
            && (skillId is null || item.Skill.Descriptor.Id.Equals(skillId, StringComparison.OrdinalIgnoreCase)));
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
            var activated = assignment.Skill is IConditionalActivationSkill conditional
                ? conditional.TryActivate(context)
                : ActivateUnconditionally(assignment.Skill, context);
            if (!activated)
            {
                return false;
            }

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

    private static bool ActivateUnconditionally(ISkill skill, in SkillContext context)
    {
        skill.OnActivated(context);
        return true;
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

                if (!CanSelectSkill(
                        forcedSkill,
                        player,
                        selected,
                        eligiblePlayers,
                        plan,
                        ignoreServerLimit: true))
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

        var granted = new List<ISkill>(selected.Count);
        var failedSkillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in selected)
        {
            if (GrantSkill(session, player, skill))
            {
                granted.Add(skill);
            }
            else
            {
                failedSkillIds.Add(skill.Descriptor.Id);
            }
        }

        // A skill can fail during OnGranted even after it passed selection. Fill
        // the vacated slot so special plans such as SkillMaster still grant five.
        while (granted.Count < requestedCount)
        {
            var candidates = GetCandidates(
                player,
                session,
                granted,
                eligiblePlayers,
                plan,
                failedSkillIds);
            var replacement = RaritySelector.Select(
                candidates,
                GetRarity,
                GetRarityWeight,
                GetWeight);
            if (replacement is null)
            {
                break;
            }

            if (GrantSkill(session, player, replacement))
            {
                granted.Add(replacement);
            }
            else
            {
                failedSkillIds.Add(replacement.Descriptor.Id);
            }
        }

        foreach (var skill in granted)
        {
            Remember(session, skill.Descriptor.Id);
        }

        if (granted.Count < requestedCount)
        {
            _plugin.Logger.LogWarning(
                "Only {GrantedCount} of {RequestedCount} skills could be granted to slot {Slot}",
                granted.Count,
                requestedCount,
                player.Slot);
        }
    }

    private bool GrantSkill(PlayerSession session, CCSPlayerController player, ISkill skill)
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
            return true;
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
            return false;
        }
    }

    private IReadOnlyCollection<ISkill> GetCandidates(
        CCSPlayerController player,
        PlayerSession session,
        IReadOnlyCollection<ISkill> selected,
        IReadOnlyCollection<CCSPlayerController> eligiblePlayers,
        SkillPlan plan,
        IReadOnlySet<string>? excludedSkillIds = null)
    {
        var selectedIds = selected
            .Select(skill => skill.Descriptor.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedTags = selected
            .SelectMany(skill => skill.Descriptor.ConflictTags)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var compatible = _registry.All
            .Where(skill => !selectedIds.Contains(skill.Descriptor.Id))
            .Where(skill => excludedSkillIds?.Contains(skill.Descriptor.Id) != true)
            .Where(skill => plan.ForcedMode != ForcedSkillMode.PoolOnly || plan.ForcedSkillIds.Contains(skill.Descriptor.Id, StringComparer.OrdinalIgnoreCase))
            .Where(skill => CanSelectSkill(skill, player, selected, eligiblePlayers, plan, usedTags))
            .ToArray();

        var preferred = compatible
            .Where(skill => !session.HasRecentSkill(skill.Descriptor.Id))
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
        IReadOnlySet<string>? usedTags = null,
        bool ignoreServerLimit = false)
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
            && (ignoreServerLimit || IsBelowServerLimit(skill, selected));
    }

    private bool CanGrantRuntimeSkill(
        ISkill skill,
        CCSPlayerController player,
        IReadOnlyCollection<CCSPlayerController> eligiblePlayers,
        bool ignoreServerLimit = false)
    {
        if (!IsEnabled(skill) || GetWeight(skill) <= 0)
        {
            return false;
        }

        if (_currentPlan is not null
            && !CompatibilityResolver.IsSkillCompatible(skill.Descriptor, _currentPlan))
        {
            return false;
        }

        return IsEligibleForPlayer(skill, player, eligiblePlayers)
            && (ignoreServerLimit || IsBelowServerLimit(skill, Array.Empty<ISkill>()));
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
        var reservedCount = _choiceReservations.Values.Count(skillIds => skillIds.Contains(id));
        return HasServerCapacity(limit, activeCount, selectedCount, reservedCount);
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
            RevokeAssignment(session, assignment, player);
        }
    }

    private void RevokeAssignment(
        PlayerSession session,
        SkillAssignment assignment,
        CCSPlayerController? player)
    {
        if (!session.Assignments.Remove(assignment))
        {
            return;
        }

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

    private SkillContext CreateContext(CCSPlayerController player, SkillAssignment assignment)
    {
        return new SkillContext(_plugin, player, assignment.Effects, assignment.State);
    }

    private void Remember(PlayerSession session, string skillId)
    {
        session.RememberSkillThisRound(skillId);
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

    private static CCSPlayerController[] SelectOneRandomPlayerPerTeam(
        IReadOnlyCollection<CCSPlayerController> eligiblePlayers)
    {
        var selected = new List<CCSPlayerController>(2);
        foreach (var team in new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist })
        {
            var candidates = eligiblePlayers.Where(player => player.Team == team).ToArray();
            if (candidates.Length > 0)
            {
                selected.Add(candidates[Random.Shared.Next(candidates.Length)]);
            }
        }

        return selected.ToArray();
    }

    private static string TeamName(CsTeam team) => team switch
    {
        CsTeam.Terrorist => "T 方",
        CsTeam.CounterTerrorist => "CT 方",
        _ => "队伍"
    };
}
