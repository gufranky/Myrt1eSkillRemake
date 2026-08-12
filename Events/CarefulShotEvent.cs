using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class CarefulShotEvent : RoundEventBase, IRoundEventWeaponFire, IRoundEventPlayerHurt
{
    private sealed class PendingShot
    {
        public required CCSPlayerController Player { get; init; }
        public required CBasePlayerWeapon Weapon { get; init; }
        public bool HitPlayer { get; set; }
    }

    private readonly Dictionary<int, PendingShot> _pending = new();
    private bool _active;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "CarefulShot",
        DisplayName = "🔫 小心开枪",
        Description = "如果射击没有命中玩家，将丢弃当前武器。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "miss-shot-penalty"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        _pending.Clear();
        context.Effects.RegisterCleanup(() =>
        {
            _active = false;
            _pending.Clear();
        });
        PrintToChatAll("[娱乐事件] 🔫 小心开枪：射击未命中玩家将丢弃当前武器！");
    }

    public void OnWeaponFire(in RoundEventContext context, EventWeaponFire @event)
    {
        if (!_active
            || @event.Userid is not { IsValid: true, PawnIsAlive: true } player
            || !IsFirearm(@event.Weapon))
        {
            return;
        }

        var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (weapon is not { IsValid: true })
        {
            return;
        }

        var shot = new PendingShot { Player = player, Weapon = weapon };
        _pending[player.Slot] = shot;
        context.Effects.AddTimer(0.12f, () =>
        {
            if (!_active
                || !_pending.TryGetValue(player.Slot, out var current)
                || !ReferenceEquals(current, shot))
            {
                return;
            }

            _pending.Remove(player.Slot);
            if (shot.HitPlayer || !player.IsValid || !player.PawnIsAlive)
            {
                return;
            }

            if (shot.Weapon.IsValid)
            {
                if (player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value?.Index == shot.Weapon.Index)
                {
                    player.DropActiveWeapon();
                }
                else
                {
                    shot.Weapon.Remove();
                }
            }
            PluginText.Center(player, "🔫 未命中！当前武器已丢弃。");
        });
    }

    public void OnPlayerHurt(in RoundEventContext context, EventPlayerHurt @event)
    {
        if (!_active
            || @event.Attacker is not { IsValid: true } attacker
            || @event.Userid is not { IsValid: true } victim
            || attacker.Slot == victim.Slot
            || @event.DmgHealth <= 0 && @event.DmgArmor <= 0)
        {
            return;
        }

        if (_pending.TryGetValue(attacker.Slot, out var shot))
        {
            shot.HitPlayer = true;
        }
    }

    private static bool IsFirearm(string? weapon) =>
        !string.IsNullOrWhiteSpace(weapon)
        && !weapon.Contains("knife", StringComparison.OrdinalIgnoreCase)
        && !weapon.Contains("bayonet", StringComparison.OrdinalIgnoreCase)
        && !weapon.Contains("grenade", StringComparison.OrdinalIgnoreCase)
        && !weapon.Contains("molotov", StringComparison.OrdinalIgnoreCase)
        && !weapon.Contains("incgrenade", StringComparison.OrdinalIgnoreCase)
        && !weapon.Contains("smokegrenade", StringComparison.OrdinalIgnoreCase)
        && !weapon.Contains("decoy", StringComparison.OrdinalIgnoreCase)
        && !weapon.Contains("c4", StringComparison.OrdinalIgnoreCase)
        && !weapon.Contains("healthshot", StringComparison.OrdinalIgnoreCase);
}
