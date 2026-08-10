using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Core;

internal sealed class PlayerSession
{
    public required int Slot { get; init; }
    public required uint ControllerIndex { get; init; }
    public required ulong SteamId { get; init; }
    public Queue<string> RecentSkills { get; } = new();
    public List<SkillAssignment> Assignments { get; } = new();

    public bool Matches(CCSPlayerController player)
    {
        if (SteamId != 0 || player.SteamID != 0)
        {
            return SteamId == player.SteamID;
        }

        return ControllerIndex == player.Index;
    }
}

internal sealed class SkillAssignment
{
    public required ISkill Skill { get; init; }
    public required EffectScope Effects { get; init; }
    public required SkillStateBag State { get; init; }
    public DateTime CooldownEndsAt { get; set; } = DateTime.MinValue;
}
