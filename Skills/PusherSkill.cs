using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class PusherSkill : ISkill, IPlayerHurtSkill
{
    private sealed record PusherState(float Chance);

    private readonly PusherSettings _settings;

    public PusherSkill(PusherSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Pusher",
        DisplayName = "🫸 推手",
        Description = "攻击敌人时有 30%～40% 的随机概率将其推开。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "on-hit-knockback-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var configuredMinimum = float.IsFinite(_settings.MinimumChance)
            ? _settings.MinimumChance
            : 0.30f;
        var configuredMaximum = float.IsFinite(_settings.MaximumChance)
            ? _settings.MaximumChance
            : 0.40f;
        var minimum = Math.Clamp(Math.Min(configuredMinimum, configuredMaximum), 0.0f, 1.0f);
        var maximum = Math.Clamp(Math.Max(configuredMinimum, configuredMaximum), minimum, 1.0f);
        var chance = minimum + Random.Shared.NextSingle() * (maximum - minimum);
        context.State.Set(new PusherState(chance));
        PluginText.Chat(context.Player, $"[推手] 本回合击退触发概率为 {chance:P0}。");
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerHurt(in SkillContext context, EventPlayerHurt @event)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        if ((@event.DmgHealth <= 0 && @event.DmgArmor <= 0)
            || attacker is not { IsValid: true, PawnIsAlive: true }
            || victim is not { IsValid: true, PawnIsAlive: true }
            || attacker.Index != context.Player.Index
            || attacker.Index == victim.Index
            || attacker.Team == victim.Team
            || !context.State.TryGet<PusherState>(out var state)
            || Random.Shared.NextSingle() > state.Chance)
        {
            return;
        }

        var attackerPawn = attacker.PlayerPawn.Value;
        var victimPawn = victim.PlayerPawn.Value;
        if (attackerPawn is not { IsValid: true } || victimPawn is not { IsValid: true })
        {
            return;
        }

        var forward = Forward(attackerPawn.EyeAngles);
        var pushVelocity = PositiveFiniteOr(_settings.PushVelocity, 400.0f);
        var jumpVelocity = PositiveFiniteOr(_settings.JumpVelocity, 300.0f);
        victimPawn.AbsVelocity.X = forward.X * pushVelocity;
        victimPawn.AbsVelocity.Y = forward.Y * pushVelocity;
        victimPawn.AbsVelocity.Z += jumpVelocity;
        Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_vecAbsVelocity");
    }

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
