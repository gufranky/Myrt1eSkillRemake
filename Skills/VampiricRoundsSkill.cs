using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class VampiricRoundsSkill : ISkill, IPlayerHurtSkill
{
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "VampiricRounds",
        DisplayName = "吸血弹",
        Description = "对敌人造成伤害时，恢复伤害值 20% 的生命。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Rare,
        DefaultWeight = 10,
        MaxPerServer = 4,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "on-hit-healing"
        }
    };

    public void OnGranted(in SkillContext context)
    {
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerHurt(in SkillContext context, EventPlayerHurt @event)
    {
        var attacker = @event.Attacker;
        if (attacker is null || !attacker.IsValid || attacker.Slot != context.Player.Slot)
        {
            return;
        }

        var victim = @event.Userid;
        if (victim is null || !victim.IsValid || victim.Team == attacker.Team)
        {
            return;
        }

        var pawn = attacker.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid || pawn.Health <= 0)
        {
            return;
        }

        var healing = Math.Max(1, (int)MathF.Ceiling(@event.DmgHealth * 0.2f));
        pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + healing);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }
}

