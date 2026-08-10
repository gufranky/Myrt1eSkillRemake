using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class QuickShotSkill : ISkill, ITickSkill
{
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "QuickShot",
        DisplayName = "速射",
        Description = "所有子弹都以极快的速度射出。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-fire-rate-control",
            "recoil-control"
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

    public void OnTick(in SkillContext context)
    {
        if (!context.Player.PawnIsAlive)
        {
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        var weapon = pawn?.WeaponServices?.ActiveWeapon.Value;
        if (pawn is null || !pawn.IsValid || weapon is null || !weapon.IsValid)
        {
            return;
        }

        if (pawn.AimPunchServices is not null)
        {
            pawn.AimPunchServices.PredictableBaseTick = 0;
            pawn.AimPunchServices.PredictableBaseTickInterpAmount = 0.0f;
            pawn.AimPunchServices.UnpredictableBaseTick = 0;
        }

        if (pawn.CameraServices is not null)
        {
            pawn.CameraServices.CsViewPunchAngleTick = 0;
            pawn.CameraServices.CsViewPunchAngleTickRatio = 0.0f;
        }

        weapon.NextPrimaryAttackTick = Server.TickCount;
        weapon.NextSecondaryAttackTick = Server.TickCount;
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
    }
}
