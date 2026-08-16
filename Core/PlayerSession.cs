using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Core;

internal sealed class PlayerSession
{
    public required int Slot { get; init; }
    public required uint ControllerIndex { get; init; }
    public required ulong SteamId { get; init; }
    // One entry per round. Multi-skill rounds therefore consume one history
    // slot rather than prematurely exhausting the anti-repeat window.
    public Queue<HashSet<string>> RecentSkillRounds { get; } = new();
    // Each player receives a private, weighted 50-skill sequence. It survives
    // round changes and is rebuilt only after every entry has been consumed.
    public NonRepeatingSequence<ISkill> SkillSequence { get; } = new();
    public List<SkillAssignment> Assignments { get; } = new();

    public bool Matches(CCSPlayerController player)
    {
        // Steam authentication can briefly report zero during connect/team
        // transitions. If either side is unauthenticated, keep the session
        // bound to the controller slot instead of discarding its cooldown
        // history and allowing an immediate repeat.
        if (SteamId != 0 && player.SteamID != 0)
        {
            return SteamId == player.SteamID;
        }

        return ControllerIndex == player.Index;
    }

    public bool HasRecentSkill(string skillId) => RecentSkillRounds.Any(round => round.Contains(skillId));

    public void BeginSkillRound(int limit)
    {
        RecentSkillRounds.Enqueue(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        while (RecentSkillRounds.Count > Math.Max(0, limit))
        {
            RecentSkillRounds.Dequeue();
        }
    }

    public void RememberSkillThisRound(string skillId)
    {
        if (RecentSkillRounds.TryPeek(out var currentRound))
        {
            currentRound.Add(skillId);
        }
    }
}

internal sealed class SkillAssignment
{
    public required ISkill Skill { get; init; }
    public required EffectScope Effects { get; init; }
    public required SkillStateBag State { get; init; }
    public DateTime CooldownEndsAt { get; set; } = DateTime.MinValue;
}
