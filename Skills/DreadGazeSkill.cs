using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

/// <summary>
/// Enemies who can see and look directly at the holder receive a short black
/// Fade. The message expires by itself, so looking away never leaves a stuck
/// overlay and does not clear an unrelated persistent Darkness effect.
/// </summary>
public sealed class DreadGazeSkill : ISkill, ITickSkill
{
    public const int FadeDuration = 64;
    public const int FadeHoldTime = 64;
    public const int FadeInFlag = 1;

    private readonly DreadGazeSettings _settings;

    public DreadGazeSkill(DreadGazeSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "DreadGaze",
        DisplayName = "👁️ 凝视深渊",
        Description = "直视你的敌人会被黑暗笼罩；移开视线后恢复。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Rare,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "screen-fade-vision"
        }
    };

    public void OnGranted(in SkillContext context)
    {
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        var refreshTicks = Math.Clamp(_settings.RefreshTicks, 1, 64);
        if (Server.TickCount % refreshTicks != 0 || !IsAlive(context.Player))
        {
            return;
        }

        var holder = context.Player;
        var holderPawn = holder.PlayerPawn.Value;
        var holderOrigin = holderPawn?.AbsOrigin;
        if (holderPawn is not { IsValid: true } || holderOrigin is null)
        {
            return;
        }

        var holderEye = EyePosition(holderPawn, holderOrigin);
        var maximumDistance = FiniteOr(_settings.MaximumDistance, 2500.0f, 0.0f, 10000.0f);
        var maximumDistanceSquared = maximumDistance * maximumDistance;
        var angle = FiniteOr(_settings.LookAngleDegrees, 8.0f, 0.25f, 45.0f);
        var minimumDot = MinimumLookDot(angle);

        foreach (var enemy in Utilities.GetPlayers())
        {
            if (!IsAlive(enemy)
                || enemy.Index == holder.Index
                || enemy.Team == holder.Team
                || !CanSee(enemy, holderPawn))
            {
                continue;
            }

            var enemyPawn = enemy.PlayerPawn.Value;
            var enemyOrigin = enemyPawn?.AbsOrigin;
            if (enemyPawn is not { IsValid: true } || enemyOrigin is null)
            {
                continue;
            }

            var enemyEye = EyePosition(enemyPawn, enemyOrigin);
            var direction = Subtract(holderEye, enemyEye);
            var distanceSquared = LengthSquared(direction);
            if (distanceSquared <= 0.0001f || distanceSquared > maximumDistanceSquared)
            {
                continue;
            }

            var lookDot = Dot(Forward(enemyPawn.EyeAngles), Normalize(direction));
            if (lookDot >= minimumDot)
            {
                SendDarkFade(enemy, Math.Clamp(_settings.Alpha, 0, 255));
            }
        }
    }

    public void OnRevoked(in SkillContext context)
    {
        // The short Fade expires naturally. Sending a purge here could clear a
        // Darkness effect owned by another skill, so no global clear is sent.
    }

    public static float MinimumLookDot(float angleDegrees) =>
        MathF.Cos(Math.Clamp(angleDegrees, 0.0f, 180.0f) * MathF.PI / 180.0f);

    private static void SendDarkFade(CCSPlayerController target, int alpha)
    {
        using var message = UserMessage.FromPartialName("Fade");
        if (message is null)
        {
            return;
        }

        message.SetInt("duration", FadeDuration);
        message.SetInt("hold_time", FadeHoldTime);
        message.SetInt("flags", FadeInFlag);
        message.SetInt("color", DarknessService.PackColor(0, 0, 0, alpha));
        message.Send(target);
    }

    private static bool CanSee(CCSPlayerController observer, CCSPlayerPawn targetPawn)
    {
        if (observer.Slot < 0)
        {
            return false;
        }

        var chunk = observer.Slot / 32;
        var masks = targetPawn.EntitySpottedState.SpottedByMask;
        if (chunk < 0 || chunk >= masks.Length)
        {
            return false;
        }

        var mask = 1u << (observer.Slot % 32);
        return (masks[chunk] & mask) != 0;
    }

    private static Vector EyePosition(CCSPlayerPawn pawn, Vector origin) =>
        new(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);

    private static Vector Forward(QAngle angle)
    {
        var pitch = angle.X * MathF.PI / 180.0f;
        var yaw = angle.Y * MathF.PI / 180.0f;
        var cosinePitch = MathF.Cos(pitch);
        return new Vector(cosinePitch * MathF.Cos(yaw), cosinePitch * MathF.Sin(yaw), -MathF.Sin(pitch));
    }

    private static Vector Subtract(Vector left, Vector right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    private static float LengthSquared(Vector vector) =>
        vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;

    private static Vector Normalize(Vector vector)
    {
        var inverseLength = 1.0f / MathF.Sqrt(LengthSquared(vector));
        return new Vector(vector.X * inverseLength, vector.Y * inverseLength, vector.Z * inverseLength);
    }

    private static float Dot(Vector left, Vector right) =>
        left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    private static float FiniteOr(float value, float fallback, float minimum, float maximum) =>
        float.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;

    private static bool IsAlive(CCSPlayerController? player) =>
        player is { IsValid: true, PawnIsAlive: true }
        && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist;
}
