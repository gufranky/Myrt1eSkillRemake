using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class ReloadTeleportEvent : RoundEventBase, IRoundEventWeaponReload
{
    private readonly NavMeshService _navMesh;

    public ReloadTeleportEvent(NavMeshService navMesh)
    {
        _navMesh = navMesh;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "ReloadTeleport",
        DisplayName = "🔄 换弹传送",
        Description = "玩家完成换弹后，会被安全地随机传送到地图上的可达位置。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "reload-behavior-rules",
            "player-teleport-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-teleport-control"
        }
    };

    public void OnWeaponReload(in RoundEventContext context, EventWeaponReload @event)
    {
        var player = @event.Userid;
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        // EventWeaponReload is emitted for an actual reload completion in the
        // current router. NextFrame lets the weapon state settle before moving.
        Server.NextFrame(() =>
        {
            if (!player.IsValid || !player.PawnIsAlive)
            {
                return;
            }

            if (_navMesh.TryTeleportRandom(player, out _))
            {
                PluginText.Center(player, "🔄 换弹传送！");
            }
        });
    }
}
