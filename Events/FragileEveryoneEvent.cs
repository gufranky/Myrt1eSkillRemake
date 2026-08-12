using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class FragileEveryoneEvent : RoundEventBase
{
    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "FragileEveryone",
        DisplayName = "大家都很脆弱",
        Description = "所有玩家本回合都获得特殊心脏技能。",
        DefaultWeight = 10,
        CanBeNested = false,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "skill-availability" }
    };

    public override void Contribute(RoundPlanBuilder builder) => builder.ReplaceAllSkills("SpecialHeart");

    public override void OnApplied(in RoundEventContext context) =>
        PrintToChatAll("[娱乐事件] 大家都很脆弱：所有人都获得特殊心脏，保护好自己的蓝色小鸡！");
}
