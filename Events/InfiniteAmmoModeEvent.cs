using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

/// <summary>
/// Enables CS2's server-authoritative infinite-ammo mode for the whole round.
/// The previous ConVar value is restored by the event effect scope.
/// </summary>
public sealed class InfiniteAmmoModeEvent : RoundEventBase
{
    public const int InfiniteAmmoValue = 1;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "InfiniteAmmoMode",
        DisplayName = "🔥 无限弹药",
        Description = "所有玩家都拥有无限弹药，无需换弹！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "global-ammo-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-ammo-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        ConVarOverrides.Set(context.Effects, "sv_infinite_ammo", InfiniteAmmoValue);
        PrintToChatAll("[娱乐事件] 🔥 无限弹药：所有玩家拥有无限弹药，无需换弹！");
    }
}
