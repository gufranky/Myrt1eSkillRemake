using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class SkillMasterEvent : RoundEventBase
{
    public const int ChampionSkillCount = 5;
    public const int ChampionMaxActiveSkillCount = 1;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "SkillMaster",
        DisplayName = "⭐ 我是达人",
        Description = "T、CT 双方各随机选出一名达人获得 5 个技能，其他人不获得技能！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "skill-availability",
            "skill-assignment-targets"
        }
    };

    public override void Contribute(RoundPlanBuilder builder) =>
        builder.AssignOneRandomPlayerPerTeam(ChampionSkillCount, ChampionMaxActiveSkillCount);

    public override void OnApplied(in RoundEventContext context) =>
        PrintToChatAll("[娱乐事件] ⭐ 我是达人：双方各有一名随机玩家获得 5 个技能，其他人没有技能！");
}
