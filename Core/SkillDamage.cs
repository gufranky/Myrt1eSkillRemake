using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;

namespace Myrt1eSkill_Remake.Core;

public static class SkillDamage
{
    public static bool TryDeal(
        CCSPlayerController attacker,
        CCSPlayerController victim,
        float damage,
        DamageTypes_t damageType = DamageTypes_t.DMG_GENERIC)
    {
        var attackerPawn = attacker.PlayerPawn.Value;
        var victimPawn = victim.PlayerPawn.Value;
        if (!attacker.IsValid
            || !victim.IsValid
            || attackerPawn is not { IsValid: true }
            || victimPawn is not { IsValid: true }
            || victimPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE
            || !float.IsFinite(damage)
            || damage <= 0.0f)
        {
            return false;
        }

        nint damageInfoPointer = nint.Zero;
        nint damageResultPointer = nint.Zero;
        try
        {
            var damageInfoSize = Schema.GetClassSize("CTakeDamageInfo");
            damageInfoPointer = Marshal.AllocHGlobal(damageInfoSize);
            ZeroMemory(damageInfoPointer, damageInfoSize);

            var damageInfo = new CTakeDamageInfo(damageInfoPointer)
            {
                Damage = damage,
                BitsDamageType = damageType
            };
            Schema.SetSchemaValue(
                damageInfo.Handle,
                "CTakeDamageInfo",
                "m_hInflictor",
                attacker.PlayerPawn.Raw);
            Schema.SetSchemaValue(
                damageInfo.Handle,
                "CTakeDamageInfo",
                "m_hAttacker",
                attacker.PlayerPawn.Raw);

            var damageResultSize = Schema.GetClassSize("CTakeDamageResult");
            damageResultPointer = Marshal.AllocHGlobal(damageResultSize);
            ZeroMemory(damageResultPointer, damageResultSize);

            var damageResult = new CTakeDamageResult(damageResultPointer)
            {
                HealthBefore = victimPawn.Health,
                HealthLost = (int)MathF.Ceiling(damage),
                DamageDealt = damage,
                PreModifiedDamage = damage,
                TotalledHealthLost = (int)MathF.Ceiling(damage),
                TotalledDamageDealt = damage,
                WasDamageSuppressed = false
            };
            Schema.SetSchemaValue(
                damageResult.Handle,
                "CTakeDamageResult",
                "m_pOriginatingInfo",
                damageInfo.Handle);

#pragma warning disable CS0618 // CounterStrikeSharp currently exposes no replacement for actively inflicting damage.
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Invoke(victimPawn, damageInfo, damageResult);
#pragma warning restore CS0618
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (damageInfoPointer != nint.Zero)
            {
                Marshal.FreeHGlobal(damageInfoPointer);
            }

            if (damageResultPointer != nint.Zero)
            {
                Marshal.FreeHGlobal(damageResultPointer);
            }
        }
    }

    private static void ZeroMemory(nint pointer, int size)
    {
        for (var offset = 0; offset < size; offset++)
        {
            Marshal.WriteByte(pointer, offset, 0);
        }
    }
}
