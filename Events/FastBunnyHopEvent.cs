using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

/// <summary>
/// Enables server-authoritative automatic bunny hopping for every player.
/// Each ConVar is restored through the event effect scope.
/// </summary>
public sealed class FastBunnyHopEvent : RoundEventBase
{
    private readonly FastBunnyHopSettings _settings;

    public FastBunnyHopEvent(FastBunnyHopSettings settings)
    {
        _settings = settings;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "FastBunnyHop",
        DisplayName = "全员快速连跳",
        Description = "所有玩家按住跳跃键即可自动快速连跳。",
        DefaultWeight = 8,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bunnyhop-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "jump-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        ConVarOverrides.Set(context.Effects, "sv_autobunnyhopping", true);
        ConVarOverrides.Set(context.Effects, "sv_enablebunnyhopping", true);
        ConVarOverrides.Set(context.Effects, "sv_staminajumpcost", 0.0f);
        ConVarOverrides.Set(context.Effects, "sv_staminalandcost", 0.0f);
        ConVarOverrides.Set(context.Effects, "sv_airaccelerate", Math.Max(0.0f, _settings.AirAccelerate));

        PrintToChatAll("[娱乐事件] 全员快速连跳：按住空格即可自动连续跳跃！");
    }
}
