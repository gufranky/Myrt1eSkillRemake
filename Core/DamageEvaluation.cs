using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace Myrt1eSkill_Remake.Core;

public static class DamageEvaluation
{
    private const float DefaultHeadshotMultiplier = 4.0f;

    public static bool WouldBeLethal(CCSPlayerPawn victim, CTakeDamageInfo damageInfo)
    {
        if (damageInfo.Damage <= 0.0f || IsFriendlyFireBlocked(victim, damageInfo))
        {
            return false;
        }

        var effectiveDamage = damageInfo.Damage;
        if (damageInfo.GetHitGroup() == HitGroup_t.HITGROUP_HEAD)
        {
            effectiveDamage *= GetHeadshotMultiplier(damageInfo);
        }

        return effectiveDamage >= victim.Health;
    }

    private static bool IsFriendlyFireBlocked(CCSPlayerPawn victim, CTakeDamageInfo damageInfo)
    {
        var attacker = damageInfo.Attacker?.Value;
        if (attacker is null || !attacker.IsValid || attacker.Handle == victim.Handle)
        {
            return false;
        }

        var attackerPawn = attacker.As<CCSPlayerPawn>();
        if (attackerPawn is null
            || !attackerPawn.IsValid
            || attackerPawn.DesignerName != "player"
            || attackerPawn.TeamNum != victim.TeamNum)
        {
            return false;
        }

        var friendlyFire = ConVar.Find("mp_friendlyfire")?.GetPrimitiveValue<bool>() ?? false;
        var teammatesAreEnemies = ConVar.Find("mp_teammates_are_enemies")?.GetPrimitiveValue<bool>() ?? false;
        return !friendlyFire && !teammatesAreEnemies;
    }

    private static float GetHeadshotMultiplier(CTakeDamageInfo damageInfo)
    {
        var ability = damageInfo.Ability?.Value;
        var weapon = ability is { IsValid: true } ? ability.As<CCSWeaponBase>() : null;
        var weaponData = weapon is { IsValid: true } ? weapon.GetVData<CCSWeaponBaseVData>() : null;
        return weaponData is { HeadshotMultiplier: > 0.0f }
            ? weaponData.HeadshotMultiplier
            : DefaultHeadshotMultiplier;
    }
}
