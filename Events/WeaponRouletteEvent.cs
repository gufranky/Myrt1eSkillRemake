using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class WeaponRouletteEvent : RoundEventBase
{
    private readonly WeaponRouletteEventSettings _settings;
    private string[] _weapons = [];
    private bool _active;

    public WeaponRouletteEvent(WeaponRouletteEventSettings settings) => _settings = settings;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "WeaponRoulette",
        DisplayName = "🎰 武器轮盘",
        Description = "每隔一段时间，所有存活玩家的主武器都会被随机替换。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-loadout-rules"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _weapons = _settings.PrimaryWeapons
            .Where(name => !string.IsNullOrWhiteSpace(name)
                           && name.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (_weapons.Length == 0)
        {
            PrintToChatAll("[娱乐事件] 武器轮盘：武器池为空，本回合不会生效。");
            return;
        }

        _active = true;
        context.Effects.RegisterCleanup(() => _active = false);
        var interval = float.IsFinite(_settings.IntervalSeconds)
            ? Math.Max(3.0f, _settings.IntervalSeconds)
            : 20.0f;
        context.Effects.AddTimer(interval, RotateAllPlayers, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        PrintToChatAll($"[娱乐事件] 🎰 武器轮盘：每 {interval:0.#} 秒随机更换所有人的主武器！");
    }

    private void RotateAllPlayers()
    {
        if (!_active || _weapons.Length == 0)
        {
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            ReplacePrimaryWeapon(player);
        }

        PrintToChatAll("[娱乐事件] 🎰 武器轮盘转动！主武器已全部更换。");
    }

    private void ReplacePrimaryWeapon(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var weapons = player.PlayerPawn.Value?.WeaponServices?.MyWeapons;
        if (weapons is null)
        {
            return;
        }

        foreach (var handle in weapons.ToArray())
        {
            var weapon = handle.Value;
            if (weapon is { IsValid: true } && IsPrimaryWeapon(weapon))
            {
                weapon.Remove();
            }
        }

        player.GiveNamedItem(_weapons[Random.Shared.Next(_weapons.Length)]);
    }

    private static bool IsPrimaryWeapon(CBasePlayerWeapon weapon)
    {
        var type = weapon.As<CCSWeaponBase>().VData?.WeaponType;
        return type is CSWeaponType.WEAPONTYPE_SUBMACHINEGUN
            or CSWeaponType.WEAPONTYPE_RIFLE
            or CSWeaponType.WEAPONTYPE_SHOTGUN
            or CSWeaponType.WEAPONTYPE_SNIPER_RIFLE
            or CSWeaponType.WEAPONTYPE_MACHINEGUN;
    }
}
