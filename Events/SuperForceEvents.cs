using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class SuperKnockbackEvent : RoundEventBase, IRoundEventPlayerHurt
{
    private readonly SuperKnockbackSettings _settings;

    public SuperKnockbackEvent(SuperKnockbackSettings settings)
    {
        _settings = settings;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "SuperKnockback",
        DisplayName = "💪 超强推背",
        Description = "对敌人造成伤害时会将其强力击飞。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "damage-knockback-rules"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        PrintToChatAll("[娱乐事件] 💪 超强推背：对敌人造成伤害时会将其强力击飞！");
    }

    public void OnPlayerHurt(in RoundEventContext context, EventPlayerHurt @event)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        if ((@event.DmgHealth <= 0 && @event.DmgArmor <= 0)
            || attacker is not { IsValid: true, PawnIsAlive: true }
            || victim is not { IsValid: true, PawnIsAlive: true }
            || attacker.Slot == victim.Slot
            || attacker.Team == victim.Team)
        {
            return;
        }

        var attackerPawn = attacker.PlayerPawn.Value;
        var victimPawn = victim.PlayerPawn.Value;
        var attackerOrigin = attackerPawn?.AbsOrigin;
        var victimOrigin = victimPawn?.AbsOrigin;
        if (attackerPawn is not { IsValid: true }
            || victimPawn is not { IsValid: true }
            || attackerOrigin is null
            || victimOrigin is null)
        {
            return;
        }

        var x = victimOrigin.X - attackerOrigin.X;
        var y = victimOrigin.Y - attackerOrigin.Y;
        var z = victimOrigin.Z - attackerOrigin.Z;
        var distance = MathF.Sqrt(x * x + y * y + z * z);
        if (distance < 0.001f)
        {
            return;
        }

        var force = PositiveFiniteOr(_settings.KnockbackForce, 1500.0f);
        var upwardForce = PositiveFiniteOr(_settings.UpwardForce, 200.0f);
        var maximumSpeed = PositiveFiniteOr(_settings.MaximumSpeed, 1000.0f);
        var velocity = victimPawn.AbsVelocity;
        var next = new Vector(
            velocity.X + x / distance * force,
            velocity.Y + y / distance * force,
            velocity.Z + z / distance * force + upwardForce);
        LimitSpeed(next, maximumSpeed);
        victimPawn.AbsVelocity.X = next.X;
        victimPawn.AbsVelocity.Y = next.Y;
        victimPawn.AbsVelocity.Z = next.Z;
        Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_vecAbsVelocity");
        PluginText.Center(victim, "💪 你被击飞了！");
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) ? Math.Max(0.0f, value) : fallback;

    internal static void LimitSpeed(Vector velocity, float maximumSpeed)
    {
        var speed = MathF.Sqrt(
            velocity.X * velocity.X
            + velocity.Y * velocity.Y
            + velocity.Z * velocity.Z);
        if (speed <= maximumSpeed || speed < 0.001f)
        {
            return;
        }

        var scale = maximumSpeed / speed;
        velocity.X *= scale;
        velocity.Y *= scale;
        velocity.Z *= scale;
    }
}

public sealed class SuperRecoilEvent : RoundEventBase, IRoundEventWeaponFire
{
    private static readonly string[] NonFirearms =
    {
        "knife", "bayonet", "grenade", "flashbang", "smokegrenade", "decoy",
        "molotov", "incgrenade", "c4", "healthshot"
    };

    private readonly SuperRecoilSettings _settings;

    public SuperRecoilEvent(SuperRecoilSettings settings)
    {
        _settings = settings;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "SuperRecoil",
        DisplayName = "💥 超强反冲",
        Description = "开枪时会产生超强反冲，把射击者向后弹飞。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-recoil-force-rules"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        PrintToChatAll("[娱乐事件] 💥 超强反冲：每次开枪都会把自己向后弹飞！");
    }

    public void OnWeaponFire(in RoundEventContext context, EventWeaponFire @event)
    {
        var player = @event.Userid;
        if (player is not { IsValid: true, PawnIsAlive: true } || !IsFirearm(@event.Weapon))
        {
            return;
        }

        var pawn = player.PlayerPawn.Value;
        var rotation = pawn?.AbsRotation;
        if (pawn is not { IsValid: true } || rotation is null)
        {
            return;
        }

        var radians = rotation.Y * MathF.PI / 180.0f;
        var force = PositiveFiniteOr(_settings.RecoilForce, 500.0f);
        var upwardRatio = PositiveFiniteOr(_settings.UpwardRatio, 0.30f);
        var maximumSpeed = PositiveFiniteOr(_settings.MaximumSpeed, 600.0f);
        var velocity = pawn.AbsVelocity;
        var next = new Vector(
            velocity.X - MathF.Cos(radians) * force,
            velocity.Y - MathF.Sin(radians) * force,
            velocity.Z + upwardRatio * force);
        SuperKnockbackEvent.LimitSpeed(next, maximumSpeed);
        pawn.AbsVelocity.X = next.X;
        pawn.AbsVelocity.Y = next.Y;
        pawn.AbsVelocity.Z = next.Z;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_vecAbsVelocity");
    }

    private static bool IsFirearm(string? weapon) =>
        !string.IsNullOrWhiteSpace(weapon)
        && !NonFirearms.Any(token => weapon.Contains(token, StringComparison.OrdinalIgnoreCase));

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) ? Math.Max(0.0f, value) : fallback;
}
