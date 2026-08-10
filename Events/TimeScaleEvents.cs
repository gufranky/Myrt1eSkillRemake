using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class BlitzkriegEvent : RoundEventBase
{
    private const float TimeScale = 2.0f;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "Blitzkrieg",
        DisplayName = "⚡ 闪击行动",
        Description = "游戏速度提升至2倍，一切都在加速进行！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timescale-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timescale-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        ConVarOverrides.Set(context.Effects, "host_timescale", TimeScale);
        PrintToChatAll("[娱乐事件] ⚡ 闪击行动：游戏速度提升至2倍！");
    }
}

public sealed class SlowMotionEvent : RoundEventBase
{
    private const float TimeScale = 0.75f;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "SlowMotion",
        DisplayName = "🎬 慢动作",
        Description = "游戏速度变为0.75倍！一切都变慢了！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timescale-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timescale-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        ConVarOverrides.Set(context.Effects, "host_timescale", TimeScale);
        PrintToChatAll("[娱乐事件] 🎬 慢动作：游戏速度变为0.75倍！");
    }
}
