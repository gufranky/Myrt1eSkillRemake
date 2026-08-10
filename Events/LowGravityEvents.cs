using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class LowGravityEvent : RoundEventBase
{
    private const float GravityMultiplier = 0.5f;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "LowGravity",
        DisplayName = "🌑 低重力",
        Description = "玩家可以跳得更高！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "gravity-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "gravity-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        var gravity = ConVarOverrides.GetFloat("sv_gravity");
        ConVarOverrides.Set(context.Effects, "sv_gravity", gravity * GravityMultiplier);
        PrintToChatAll("[娱乐事件] 🌑 低重力：玩家可以跳得更高！");
    }
}

public sealed class LowGravityPlusPlusEvent : RoundEventBase
{
    private const float GravityMultiplier = 0.2f;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "LowGravityPlusPlus",
        DisplayName = "🌑 超低重力",
        Description = "重力大幅降低，空中射击无扩散！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "gravity-rules",
            "weapon-spread-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "gravity-control",
            "weapon-spread-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        var gravity = ConVarOverrides.GetFloat("sv_gravity");
        ConVarOverrides.Set(context.Effects, "sv_gravity", gravity * GravityMultiplier);
        ConVarOverrides.Set(context.Effects, "weapon_accuracy_nospread", true);
        PrintToChatAll("[娱乐事件] 🌑 超低重力：重力大幅降低，本回合射击无扩散！");
    }
}
