using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

/// <summary>Everyone receives an AK with one round in the magazine and infinite reserve ammo.</summary>
public sealed class OneShotAkEvent : RoundEventBase,
    IRoundEventPlayerSpawn,
    IRoundEventItemPickup,
    IRoundEventWeaponFire,
    IRoundEventWeaponReload,
    IRoundEventTick
{
    public const string WeaponName = "weapon_ak47";

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "OneShotAK",
        DisplayName = "一发 AK",
        Description = "所有人只能使用一把 AK-47；当前弹匣只有 1 发，但备用弹药无限。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-loadout-rules", "global-ammo-rules"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        ConVarOverrides.Set(context.Effects, "mp_buy_allow_guns", 0);
        ConVarOverrides.Set(context.Effects, "mp_buytime", 0.0f);
        ConVarOverrides.Set(context.Effects, "sv_infinite_ammo", 1);
        foreach (var player in Utilities.GetPlayers())
        {
            PreparePlayer(player);
        }

        PrintToChatAll("[娱乐事件] 一发 AK：弹匣只有 1 发，备用弹药无限！");
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        context.Effects.AddTimer(0.5f, () => PreparePlayer(@event.Userid));
    }

    public void OnItemPickup(in RoundEventContext context, EventItemPickup @event)
    {
        context.Effects.AddTimer(0.01f, () => PreparePlayer(@event.Userid));
    }

    public void OnWeaponFire(in RoundEventContext context, EventWeaponFire @event)
    {
        // Do not refill here: the shot must leave the magazine empty and
        // require a real reload before the next round is available.
    }

    public void OnWeaponReload(in RoundEventContext context, EventWeaponReload @event) =>
        context.Effects.AddTimer(0.05f, () => SetActiveClip(@event.Userid));

    public void OnTick(in RoundEventContext context)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            SetActiveClip(player);
        }
    }

    private static void PreparePlayer(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var services = player.PlayerPawn.Value?.WeaponServices;
        if (services is null)
        {
            return;
        }

        var hasAk = false;
        foreach (var handle in services.MyWeapons.ToArray())
        {
            var weapon = handle.Value;
            if (weapon is not { IsValid: true })
            {
                continue;
            }

            if (weapon.DesignerName.Equals(WeaponName, StringComparison.OrdinalIgnoreCase))
            {
                hasAk = true;
            }
            else if (IsGun(weapon))
            {
                weapon.Remove();
            }
        }

        if (!hasAk)
        {
            player.GiveNamedItem(WeaponName);
        }

        var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon is { IsValid: true }
            && activeWeapon.DesignerName.Equals(WeaponName, StringComparison.OrdinalIgnoreCase))
        {
            activeWeapon.Clip1 = 1;
            Utilities.SetStateChanged(activeWeapon, "CBasePlayerWeapon", "m_iClip1");
        }
    }

    private static void SetActiveClip(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (weapon is { IsValid: true }
            && weapon.DesignerName.Equals(WeaponName, StringComparison.OrdinalIgnoreCase)
            && weapon.Clip1 > 1)
        {
            weapon.Clip1 = 1;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        }
    }

    private static bool IsGun(CBasePlayerWeapon weapon)
    {
        var type = weapon.As<CCSWeaponBase>().VData?.WeaponType;
        return type is CSWeaponType.WEAPONTYPE_PISTOL
            or CSWeaponType.WEAPONTYPE_SUBMACHINEGUN
            or CSWeaponType.WEAPONTYPE_RIFLE
            or CSWeaponType.WEAPONTYPE_SHOTGUN
            or CSWeaponType.WEAPONTYPE_SNIPER_RIFLE
            or CSWeaponType.WEAPONTYPE_MACHINEGUN;
    }
}
