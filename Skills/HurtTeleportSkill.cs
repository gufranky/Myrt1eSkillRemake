using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class HurtTeleportSkill : ISkill, IPlayerHurtSkill
{
    public const float TriggerCooldownSeconds = 0.75f;

    private sealed class HurtTeleportState
    {
        public float NextTeleportAt { get; set; }
    }

    private readonly NavMeshService _navMesh;

    public HurtTeleportSkill(NavMeshService navMesh)
    {
        _navMesh = navMesh;
    }

    public static SkillDescriptor Definition { get; } = new()
    {
        Id = "HurtTeleport",
        DisplayName = "💫 受伤传送",
        Description = "受到有效伤害后，随机传送到一个可达的安全位置。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Rare,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hit-teleport-control"
        }
    };

    public SkillDescriptor Descriptor => Definition;

    public void OnGranted(in SkillContext context) =>
        context.State.Set(new HurtTeleportState());

    public void OnPlayerHurt(in SkillContext context, EventPlayerHurt @event)
    {
        if ((@event.DmgHealth <= 0 && @event.DmgArmor <= 0)
            || @event.Userid is not { IsValid: true, PawnIsAlive: true } victim
            || victim.Slot != context.Player.Slot
            || !context.State.TryGet<HurtTeleportState>(out var state)
            || Server.CurrentTime < state.NextTeleportAt)
        {
            return;
        }

        if (!_navMesh.TryTeleportRandom(victim, out _))
        {
            return;
        }

        state.NextTeleportAt = Server.CurrentTime + TriggerCooldownSeconds;
        PluginText.Center(victim, "💫 受伤传送！");
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
