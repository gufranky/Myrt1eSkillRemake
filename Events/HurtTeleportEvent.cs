using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class HurtTeleportEvent : RoundEventBase, IRoundEventPlayerHurt
{
    private const float TeleportCooldownSeconds = 0.75f;
    private readonly NavMeshService _navMesh;
    private readonly Dictionary<int, float> _cooldowns = new();

    public HurtTeleportEvent(NavMeshService navMesh)
    {
        _navMesh = navMesh;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "HurtTeleport",
        DisplayName = "💫 受伤传送",
        Description = "所有玩家受到有效伤害后，都会传送到一个可达的安全位置！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hit-teleport-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hit-teleport-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _cooldowns.Clear();
        PrintToChatAll("[娱乐事件] 💫 受伤传送：所有人受到伤害后都会随机传送！");
    }

    public override void OnRemoved(in RoundEventContext context) => _cooldowns.Clear();

    public void OnPlayerHurt(in RoundEventContext context, EventPlayerHurt @event)
    {
        var victim = @event.Userid;
        if ((@event.DmgHealth <= 0 && @event.DmgArmor <= 0)
            || victim is not { IsValid: true, PawnIsAlive: true }
            || victim.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist)
            || IsCoolingDown(victim.Slot))
        {
            return;
        }

        if (!_navMesh.TryTeleportRandom(victim, out _))
        {
            return;
        }

        _cooldowns[victim.Slot] = Server.CurrentTime + TeleportCooldownSeconds;
        PluginText.Center(victim, "💫 受伤传送！");
    }

    private bool IsCoolingDown(int slot) =>
        _cooldowns.TryGetValue(slot, out var expiresAt) && Server.CurrentTime < expiresAt;
}
