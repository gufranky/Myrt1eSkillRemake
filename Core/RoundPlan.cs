using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public enum ForcedSkillMode
{
    None,
    EnsureIncluded,
    ReplaceAll,
    PoolOnly
}

public enum SkillAssignmentMode
{
    AllPlayers,
    OneRandomPlayerPerTeam
}

public sealed record SkillPlan
{
    public required bool Enabled { get; init; }
    public required int SlotsPerPlayer { get; init; }
    public required int MaxActiveSkillsPerPlayer { get; init; }
    public required SkillAssignmentMode AssignmentMode { get; init; }
    public required ForcedSkillMode ForcedMode { get; init; }
    public required IReadOnlyList<string> ForcedSkillIds { get; init; }
    public required IReadOnlySet<string> BlockedSkillIds { get; init; }
    public required IReadOnlySet<string> BlockedSkillTags { get; init; }
    public required IReadOnlySet<string> ActiveEventIds { get; init; }
}

public sealed record RoundPlan
{
    public required IReadOnlyList<EventDescriptor> Events { get; init; }
    public required SkillPlan Skills { get; init; }
}

public sealed class RoundPlanBuilder
{
    private readonly PluginConfig _config;
    private readonly List<EventDescriptor> _events = new();
    private readonly HashSet<string> _forcedSkills = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _blockedSkillIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _blockedSkillTags = new(StringComparer.OrdinalIgnoreCase);
    private bool _skillsEnabled;
    private int _requiredSlots;
    private int? _maxActiveSkillsOverride;
    private ForcedSkillMode _forcedMode;
    private SkillAssignmentMode _assignmentMode;

    public RoundPlanBuilder(PluginConfig config)
    {
        _config = config;
        _skillsEnabled = config.Enabled;
        _requiredSlots = config.SkillsPerPlayer;
    }

    public void SetActiveEvents(IEnumerable<IRoundEvent> events)
    {
        _events.Clear();
        _events.AddRange(events.Select(@event => @event.Descriptor));

        foreach (var descriptor in _events)
        {
            _blockedSkillTags.UnionWith(descriptor.BlockedSkillTags);
        }
    }

    public void DisableSkills()
    {
        _skillsEnabled = false;
    }

    public void RequireSkillSlots(int totalSlots)
    {
        _requiredSlots = Math.Max(_requiredSlots, totalSlots);
    }

    public void AssignOneRandomPlayerPerTeam(int skillCount, int maxActiveSkills = 1)
    {
        _assignmentMode = SkillAssignmentMode.OneRandomPlayerPerTeam;
        _requiredSlots = Math.Max(_requiredSlots, Math.Clamp(skillCount, 0, 8));
        _maxActiveSkillsOverride = Math.Clamp(maxActiveSkills, 0, 8);
    }

    public void ReplaceAllSkills(params string[] skillIds)
    {
        SetForcedSkills(ForcedSkillMode.ReplaceAll, skillIds);
    }

    public void EnsureSkills(params string[] skillIds)
    {
        if (_forcedMode != ForcedSkillMode.ReplaceAll)
        {
            SetForcedSkills(ForcedSkillMode.EnsureIncluded, skillIds);
        }
    }

    public void RestrictPoolTo(params string[] skillIds)
    {
        if (_forcedMode is not ForcedSkillMode.ReplaceAll)
        {
            SetForcedSkills(ForcedSkillMode.PoolOnly, skillIds);
        }
    }

    public void BlockSkillIds(params string[] skillIds)
    {
        _blockedSkillIds.UnionWith(skillIds.Where(id => !string.IsNullOrWhiteSpace(id)));
    }

    public void BlockSkillTags(params string[] tags)
    {
        _blockedSkillTags.UnionWith(tags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
    }

    public RoundPlan Build()
    {
        var configuredMaxSlots = Math.Clamp(_config.MaxSkillsPerPlayer, 0, 8);
        var maxSlots = _assignmentMode == SkillAssignmentMode.OneRandomPlayerPerTeam
            ? Math.Max(configuredMaxSlots, Math.Clamp(_requiredSlots, 0, 8))
            : configuredMaxSlots;
        var slots = Math.Clamp(_requiredSlots, 0, maxSlots);
        var configuredMaxActiveSkills = Math.Clamp(_config.MaxActiveSkillsPerPlayer, 0, maxSlots);
        var maxActiveSkills = _maxActiveSkillsOverride is { } activeOverride
            ? Math.Min(configuredMaxActiveSkills, activeOverride)
            : configuredMaxActiveSkills;
        var forcedMode = _skillsEnabled ? _forcedMode : ForcedSkillMode.None;
        var forcedSkills = _skillsEnabled ? _forcedSkills.ToArray() : Array.Empty<string>();

        if (forcedMode == ForcedSkillMode.ReplaceAll)
        {
            slots = Math.Min(maxSlots, forcedSkills.Length);
        }
        else if (forcedMode == ForcedSkillMode.EnsureIncluded)
        {
            slots = Math.Max(slots, Math.Min(maxSlots, forcedSkills.Length));
        }

        return new RoundPlan
        {
            Events = _events.ToArray(),
            Skills = new SkillPlan
            {
                Enabled = _skillsEnabled && slots > 0,
                SlotsPerPlayer = slots,
                MaxActiveSkillsPerPlayer = maxActiveSkills,
                AssignmentMode = _assignmentMode,
                ForcedMode = forcedMode,
                ForcedSkillIds = forcedSkills,
                BlockedSkillIds = new HashSet<string>(_blockedSkillIds, StringComparer.OrdinalIgnoreCase),
                BlockedSkillTags = new HashSet<string>(_blockedSkillTags, StringComparer.OrdinalIgnoreCase),
                ActiveEventIds = _events.Select(@event => @event.Id).ToHashSet(StringComparer.OrdinalIgnoreCase)
            }
        };
    }

    private void SetForcedSkills(ForcedSkillMode mode, IEnumerable<string> skillIds)
    {
        _forcedMode = mode;
        _forcedSkills.Clear();
        _forcedSkills.UnionWith(skillIds.Where(id => !string.IsNullOrWhiteSpace(id)));
    }
}
