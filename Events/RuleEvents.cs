using CounterStrikeSharp.API;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public abstract class RoundEventBase : IRoundEvent
{
    public abstract EventDescriptor Descriptor { get; }

    public virtual void Contribute(RoundPlanBuilder builder)
    {
    }

    public virtual void OnApplied(in RoundEventContext context)
    {
    }

    public virtual void OnRemoved(in RoundEventContext context)
    {
    }

    protected static void PrintToChatAll(string message)
    {
        PluginText.ChatAll(message);
    }
}

public sealed class NormalRoundEvent : RoundEventBase
{
    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "NormalRound",
        DisplayName = "正常回合",
        Description = "本回合没有额外娱乐事件。",
        DefaultWeight = 100,
        CanBeNested = false
    };
}

public sealed class NoSkillEvent : RoundEventBase
{
    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "NoSkill",
        DisplayName = "没有技能",
        Description = "本回合所有玩家都不会获得技能。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "skill-availability"
        }
    };

    public override void Contribute(RoundPlanBuilder builder) => builder.DisableSkills();

    public override void OnApplied(in RoundEventContext context) =>
        PrintToChatAll("[娱乐事件] 没有技能：本回合禁用所有随机技能。");
}

public sealed class MoreSkillsEvent : RoundEventBase
{
    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "MoreSkills",
        DisplayName = "更多技能",
        Description = "每位玩家至少获得两个技能。",
        DefaultWeight = 10
    };

    public override void Contribute(RoundPlanBuilder builder) => builder.RequireSkillSlots(2);

    public override void OnApplied(in RoundEventContext context) =>
        PrintToChatAll("[娱乐事件] 更多技能：每位玩家至少获得两个技能。");
}

public sealed class SkillsPlusPlusEvent : RoundEventBase
{
    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "SkillsPlusPlus",
        DisplayName = "技能++",
        Description = "每位玩家至少获得三个技能。",
        DefaultWeight = 5
    };

    public override void Contribute(RoundPlanBuilder builder) => builder.RequireSkillSlots(3);

    public override void OnApplied(in RoundEventContext context) =>
        PrintToChatAll("[娱乐事件] 技能++：每位玩家至少获得三个技能。");
}

public sealed class ChooseCarnivalEvent : RoundEventBase
{
    private readonly string _forcedSkillId;

    public ChooseCarnivalEvent(string forcedSkillId)
    {
        _forcedSkillId = forcedSkillId;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "ChooseCarnival",
        DisplayName = "选择狂欢",
        Description = "使用配置的指定技能替换普通随机技能。",
        DefaultWeight = 5,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "skill-availability",
            "skill-selection-replace"
        }
    };

    public override void Contribute(RoundPlanBuilder builder) => builder.ReplaceAllSkills(_forcedSkillId);

    public override void OnApplied(in RoundEventContext context) =>
        PrintToChatAll($"[娱乐事件] 选择狂欢：本回合指定技能为 {_forcedSkillId}。");
}

public sealed class TopTierPartyEvent : RoundEventBase
{
    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "TopTierParty",
        DisplayName = "顶级狂欢",
        Description = "额外抽取两个互相兼容的娱乐事件。",
        DefaultWeight = 3,
        CanBeNested = false,
        CompositeChildCount = 2
    };
}

public sealed class TopTierPartyPlusPlusEvent : RoundEventBase
{
    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "TopTierPartyPlusPlus",
        DisplayName = "顶级狂欢++",
        Description = "额外抽取三个互相兼容的娱乐事件。",
        DefaultWeight = 1,
        CanBeNested = false,
        CompositeChildCount = 3
    };
}
