using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class BirdshotKingEvent : RoundEventBase, IRoundEventPlayerSpawn, IRoundEventItemPickup
{
    public const string BirdshotWeapon = "weapon_ssg08";
    public const int BuyAllowGunsValue = 0;
    public const float BuyTimeValue = 0.0f;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "BirdshotKing",
        DisplayName = "🦅 鸟狙大王",
        Description = "没收所有人的主武器和副武器，发给每人一把鸟狙；关闭商店并让枪械射击无扩散。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-loadout-rules", "buy-rules", "weapon-spread-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-spread-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        ConVarOverrides.Set(context.Effects, "mp_buy_allow_guns", BuyAllowGunsValue);
        ConVarOverrides.Set(context.Effects, "mp_buytime", BuyTimeValue);
        ConVarOverrides.Set(context.Effects, "weapon_accuracy_nospread", true);
        ConVarOverrides.Set(context.Effects, "weapon_accuracy_forcespread", 0.0f);
        foreach (var player in Utilities.GetPlayers())
        {
            PreparePlayer(player);
        }
        PrintToChatAll("[娱乐事件] 🦅 鸟狙大王：所有人只能使用无扩散鸟狙，商店已关闭！");
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid;
        context.Effects.AddTimer(0.5f, () => PreparePlayer(player));
    }

    public void OnItemPickup(in RoundEventContext context, EventItemPickup @event)
    {
        var player = @event.Userid;
        context.Effects.AddTimer(0.01f, () => PreparePlayer(player));
    }

    internal static bool IsPrimaryOrSecondary(CBasePlayerWeapon weapon)
    {
        var weaponData = weapon.As<CCSWeaponBase>().VData;
        return weaponData?.WeaponType is CSWeaponType.WEAPONTYPE_PISTOL
            or CSWeaponType.WEAPONTYPE_SUBMACHINEGUN
            or CSWeaponType.WEAPONTYPE_RIFLE
            or CSWeaponType.WEAPONTYPE_SHOTGUN
            or CSWeaponType.WEAPONTYPE_SNIPER_RIFLE
            or CSWeaponType.WEAPONTYPE_MACHINEGUN;
    }

    private static void PreparePlayer(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var weaponServices = player.PlayerPawn.Value?.WeaponServices;
        if (weaponServices is null)
        {
            return;
        }

        var hasBirdshot = false;
        foreach (var handle in weaponServices.MyWeapons.ToArray())
        {
            var weapon = handle.Value;
            if (weapon is not { IsValid: true })
            {
                continue;
            }

            if (weapon.DesignerName.Equals(BirdshotWeapon, StringComparison.OrdinalIgnoreCase))
            {
                hasBirdshot = true;
            }
            else if (IsPrimaryOrSecondary(weapon))
            {
                weapon.Remove();
            }
        }

        if (!hasBirdshot)
        {
            player.GiveNamedItem(BirdshotWeapon);
        }
    }
}
