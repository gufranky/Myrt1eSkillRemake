using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class DeadlyGrenadesEvent : RoundEventBase,
    IRoundEventPlayerSpawn,
    IRoundEventGrenadeThrown,
    IRoundEventItemPickup
{
    public const int InfiniteAmmoValue = 1;
    public const int BuyAllowGunsValue = 0;
    public const float BuyTimeValue = 0.0f;

    private readonly DeadlyGrenadesSettings _settings;
    private bool _active;

    public DeadlyGrenadesEvent(DeadlyGrenadesSettings settings)
    {
        _settings = settings;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "DeadlyGrenades",
        DisplayName = "💣 更致命的手雷",
        Description = "无限高爆手雷！移除主副武器！禁用商店！手雷伤害和范围增加！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "global-ammo-rules",
            "hegrenade-rules",
            "weapon-loadout-rules",
            "buy-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-ammo-control",
            "hegrenade-behavior-control",
            "projectile-launcher-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        context.Effects.RegisterCleanup(() => _active = false);

        ConVarOverrides.Set(context.Effects, "mp_buy_allow_guns", BuyAllowGunsValue);
        ConVarOverrides.Set(context.Effects, "mp_buytime", BuyTimeValue);
        ConVarOverrides.Set(context.Effects, "sv_hegrenade_damage_multiplier", GetDamageMultiplier());
        ConVarOverrides.Set(context.Effects, "sv_hegrenade_radius_multiplier", GetRadiusMultiplier());
        ConVarOverrides.Set(context.Effects, "sv_infinite_ammo", InfiniteAmmoValue);

        foreach (var player in Utilities.GetPlayers())
        {
            PreparePlayer(player);
        }

        PrintToChatAll(
            $"[娱乐事件] 💣 更致命的手雷：无限 HE，伤害 {GetDamageMultiplier():0.##}×、范围 {GetRadiusMultiplier():0.##}×；主副武器和商店已禁用！");
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid;
        context.Effects.AddTimer(0.5f, () =>
        {
            if (_active && player is not null)
            {
                PreparePlayer(player);
                PluginText.Center(player, "💣 更致命的手雷！");
            }
        });
    }

    public void OnGrenadeThrown(in RoundEventContext context, EventGrenadeThrown @event)
    {
        if (!_active || !string.Equals(@event.Weapon, "hegrenade", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var player = @event.Userid;
        context.Effects.AddTimer(0.3f, () =>
        {
            if (_active)
            {
                EnsureHeGrenade(player);
            }
        });
    }

    public void OnItemPickup(in RoundEventContext context, EventItemPickup @event)
    {
        var player = @event.Userid;
        context.Effects.AddTimer(0.01f, () =>
        {
            if (_active)
            {
                RemovePrimaryAndSecondaryWeapons(player);
            }
        });
    }

    private void PreparePlayer(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        RemovePrimaryAndSecondaryWeapons(player);
        var count = Math.Clamp(_settings.StartingGrenadeCount, 1, 10);
        for (var index = 0; index < count; index++)
        {
            player.GiveNamedItem("weapon_hegrenade");
        }
    }

    private static void EnsureHeGrenade(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var alreadyHasGrenade = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_hegrenade" }) == true;
        if (!alreadyHasGrenade)
        {
            player.GiveNamedItem("weapon_hegrenade");
        }
    }

    private static void RemovePrimaryAndSecondaryWeapons(CCSPlayerController? player)
    {
        var weaponServices = player?.PlayerPawn.Value?.WeaponServices;
        if (player is not { IsValid: true } || weaponServices is null)
        {
            return;
        }

        foreach (var handle in weaponServices.MyWeapons.ToArray())
        {
            var weapon = handle.Value;
            var weaponData = weapon?.As<CCSWeaponBase>().VData;
            if (weapon is not { IsValid: true } || weaponData is null)
            {
                continue;
            }

            if (weaponData.WeaponType is CSWeaponType.WEAPONTYPE_PISTOL
                or CSWeaponType.WEAPONTYPE_SUBMACHINEGUN
                or CSWeaponType.WEAPONTYPE_RIFLE
                or CSWeaponType.WEAPONTYPE_SHOTGUN
                or CSWeaponType.WEAPONTYPE_SNIPER_RIFLE
                or CSWeaponType.WEAPONTYPE_MACHINEGUN)
            {
                weapon.Remove();
            }
        }
    }

    private float GetDamageMultiplier() => float.IsFinite(_settings.DamageMultiplier)
        ? Math.Max(0.0f, _settings.DamageMultiplier)
        : 3.0f;

    private float GetRadiusMultiplier() => float.IsFinite(_settings.RadiusMultiplier)
        ? Math.Max(0.0f, _settings.RadiusMultiplier)
        : 5.0f;
}
