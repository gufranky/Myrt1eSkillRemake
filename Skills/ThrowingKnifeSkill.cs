using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ThrowingKnifeSkill : ISkill, ITickSkill, IPlayerDeathSkill
{
    private sealed class ThrowingKnifeState
    {
        public bool Active { get; set; } = true;
        public bool IsThrown { get; set; }
        public CBasePlayerWeapon? Knife { get; set; }
        public CTriggerMultiple? Trigger { get; set; }
    }

    private readonly ThrowingKnifeSettings _settings;
    private readonly ThrowingKnifeService _service;

    public ThrowingKnifeSkill(ThrowingKnifeSettings settings, ThrowingKnifeService service)
    {
        _settings = settings;
        _service = service;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "ThrowingKnife",
        DisplayName = "🔪 飞刀",
        Description = "点击 [css_useskill] 掷出自己的刀；命中敌人会造成致命伤害，但其他人可以捡走它。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = 1,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "projectile-launcher-control",
            "knife-loadout-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new ThrowingKnifeState();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() =>
        {
            state.Active = false;
            _service.RemoveTrigger(state.Trigger);
            if (state.IsThrown && state.Knife is { IsValid: true })
            {
                state.Knife.Remove();
            }
        });
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<ThrowingKnifeState>(out var state) || !state.Active)
        {
            return;
        }

        if (state.IsThrown)
        {
            PluginText.Chat(context.Player, "[飞刀] 你的刀还没有取回来，小心被其他人捡走！");
            return;
        }

        context.Player.ExecuteClientCommand("slot3");
        var player = context.Player;
        var effects = context.Effects;
        effects.AddTimer(0.125f, () => TryThrow(player, effects, state));
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<ThrowingKnifeState>(out var state)
            || !state.Active
            || !state.IsThrown
            || !HasKnife(context.Player))
        {
            return;
        }

        state.IsThrown = false;
        state.Knife = null;
        _service.RemoveTrigger(state.Trigger);
        state.Trigger = null;
        PluginText.Chat(context.Player, "[飞刀] 你取回了自己的刀。");
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid?.Index != context.Player.Index
            || !context.State.TryGet<ThrowingKnifeState>(out var state))
        {
            return;
        }

        _service.RemoveTrigger(state.Trigger);
        state.Trigger = null;
        if (state.IsThrown && state.Knife is { IsValid: true })
        {
            state.Knife.Remove();
        }
        state.Knife = null;
        state.IsThrown = false;
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    private void TryThrow(CCSPlayerController player, EffectScope effects, ThrowingKnifeState state)
    {
        if (!state.Active || !player.IsValid || !player.PawnIsAlive)
        {
            return;
        }

        var pawn = player.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        var knife = pawn?.WeaponServices?.ActiveWeapon.Value;
        if (pawn is not { IsValid: true }
            || origin is null
            || knife is not { IsValid: true }
            || !IsKnife(knife.DesignerName))
        {
            PluginText.Chat(player, "[飞刀] 必须拥有并切换到刀才能投掷。");
            return;
        }

        player.DropActiveWeapon();
        effects.AddTimer(0.01f, () =>
        {
            if (!state.Active || !knife.IsValid || !pawn.IsValid)
            {
                return;
            }

            var eye = new Vector(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
            var force = PositiveFiniteOr(_settings.ThrowForce, 2000.0f);
            var velocity = Forward(pawn.EyeAngles) * force;
            knife.Teleport(eye, new QAngle(0.0f, 0.0f, 0.0f), velocity);
            if (knife.Collision is not null && pawn.Collision is not null)
            {
                knife.Collision.CollisionAttribute.InteractsWith = pawn.Collision.CollisionAttribute.InteractsWith;
                Utilities.SetStateChanged(knife, "CCollisionProperty", "m_collisionAttribute");
            }

            state.IsThrown = true;
            state.Knife = knife;
            state.Trigger = _service.CreateTrigger(
                knife,
                _settings.TriggerRadius,
                effects,
                entity => OnKnifeTouch(player, state, entity));
        });
    }

    private void OnKnifeTouch(CCSPlayerController thrower, ThrowingKnifeState state, CBaseEntity entity)
    {
        if (!state.Active
            || !state.IsThrown
            || HasKnife(thrower)
            || !string.Equals(entity.DesignerName, "player", StringComparison.Ordinal))
        {
            return;
        }

        var victimPawn = entity.As<CCSPlayerPawn>();
        var victim = victimPawn?.Controller.Value?.As<CCSPlayerController>();
        if (victimPawn is not { IsValid: true }
            || victim is not { IsValid: true, PawnIsAlive: true }
            || victim.Index == thrower.Index
            || (!_settings.FriendlyFire && victim.Team == thrower.Team))
        {
            return;
        }

        var damage = PositiveFiniteOr(_settings.Damage, 9999.0f);
        if (!SkillDamage.TryDeal(thrower, victim, damage, DamageTypes_t.DMG_SLASH))
        {
            victimPawn.Health = 0;
            Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_iHealth");
            Server.NextFrame(() =>
            {
                if (victimPawn.IsValid && victimPawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                {
                    victimPawn.CommitSuicide(false, true);
                }
            });
        }
    }

    private static bool HasKnife(CCSPlayerController player) =>
        player.IsValid
        && player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            handle => handle.Value is { IsValid: true } weapon && IsKnife(weapon.DesignerName)) == true;

    private static bool IsKnife(string name) =>
        name.Contains("knife", StringComparison.OrdinalIgnoreCase)
        || name.Contains("bayonet", StringComparison.OrdinalIgnoreCase);

    private static Vector Forward(QAngle angle)
    {
        var pitch = angle.X * MathF.PI / 180.0f;
        var yaw = angle.Y * MathF.PI / 180.0f;
        var cosinePitch = MathF.Cos(pitch);
        return new Vector(cosinePitch * MathF.Cos(yaw), cosinePitch * MathF.Sin(yaw), -MathF.Sin(pitch));
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) ? Math.Max(0.0f, value) : fallback;
}
