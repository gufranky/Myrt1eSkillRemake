using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ProsthesisSkill : ISkill, IPlayerHurtPreSkill
{
    private static readonly HashSet<HitGroup_t> ProtectedHitGroups =
    [
        HitGroup_t.HITGROUP_LEFTARM,
        HitGroup_t.HITGROUP_RIGHTARM,
        HitGroup_t.HITGROUP_LEFTLEG,
        HitGroup_t.HITGROUP_RIGHTLEG
    ];

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Prosthesis",
        DisplayName = "假肢",
        Description = "手臂和腿部防弹。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "limb-damage-immunity"
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

    public void OnPlayerHurtPre(in SkillContext context, EventPlayerHurt @event)
    {
        if (!ProtectedHitGroups.Contains((HitGroup_t)@event.Hitgroup))
        {
            return;
        }

        var attacker = @event.Attacker;
        if (attacker is null || !attacker.IsValid || attacker.Slot == context.Player.Slot)
        {
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid)
        {
            return;
        }

        if (@event.DmgHealth > 0)
        {
            pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + @event.DmgHealth);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }

        if (@event.DmgArmor > 0)
        {
            pawn.ArmorValue += @event.DmgArmor;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        }

        @event.DmgHealth = 0;
        @event.DmgArmor = 0;
    }
}
