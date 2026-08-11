using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Speeds up CChicken's native Leader navigation without replacing its path.
/// Extra movement is applied only in the direction the engine AI already
/// accepted, while stuck chickens are told to refresh their native NavMesh path.
/// </summary>
public static class ChickenMovementBoost
{
    public const int RepathAfterStuckTicks = 24;
    public const float RepathMinimumDistance = 160.0f;

    public sealed class State
    {
        public Vector? LastOrigin { get; set; }
        public int StuckTicks { get; set; }
    }

    public static void Update(
        CChicken chicken,
        Vector targetOrigin,
        State state,
        float speedMultiplier,
        float maximumExtraStep,
        float maximumSpeed = float.PositiveInfinity)
    {
        var origin = chicken.AbsOrigin;
        if (!chicken.IsValid || origin is null)
        {
            return;
        }

        var current = new Vector(origin.X, origin.Y, origin.Z);
        var previous = state.LastOrigin;
        if (previous is null)
        {
            state.LastOrigin = current;
            return;
        }

        var dx = current.X - previous.X;
        var dy = current.Y - previous.Y;
        var movement = MathF.Sqrt(dx * dx + dy * dy);
        var distanceToTargetSquared = DistanceSquared(current, targetOrigin);

        if (movement > 0.05f && movement < 32.0f)
        {
            state.StuckTicks = 0;
            var multiplier = FiniteOr(speedMultiplier, 2.5f, 1.0f, 6.0f);
            var extraDistance = CalculateExtraDistance(
                movement,
                multiplier,
                maximumExtraStep,
                maximumSpeed);
            if (extraDistance > 0.01f)
            {
                var scale = extraDistance / movement;
                var boosted = new Vector(
                    current.X + dx * scale,
                    current.Y + dy * scale,
                    current.Z);
                chicken.Teleport(boosted, chicken.AbsRotation, chicken.AbsVelocity);
                state.LastOrigin = boosted;
                return;
            }
        }
        else if (movement <= 0.05f
                 && distanceToTargetSquared > RepathMinimumDistance * RepathMinimumDistance)
        {
            state.StuckTicks++;
            if (state.StuckTicks >= RepathAfterStuckTicks)
            {
                // Expiring these timers asks the native chicken AI to update its
                // Leader path immediately. That path is backed by CS2 NavMesh.
                chicken.RepathTimer.Timestamp = 0.0f;
                chicken.MoveRateThrottleTimer.Timestamp = 0.0f;
                state.StuckTicks = 0;
            }
        }
        else
        {
            state.StuckTicks = 0;
        }

        state.LastOrigin = current;
    }

    public static float CalculateExtraDistance(
        float movement,
        float speedMultiplier,
        float maximumExtraStep,
        float maximumSpeed = float.PositiveInfinity)
    {
        var safeMovement = float.IsFinite(movement) ? Math.Max(0.0f, movement) : 0.0f;
        var multiplier = FiniteOr(speedMultiplier, 2.5f, 1.0f, 6.0f);
        var maximum = FiniteOr(maximumExtraStep, 8.0f, 0.0f, 24.0f);
        var extra = Math.Min(safeMovement * (multiplier - 1.0f), maximum);
        if (float.IsFinite(maximumSpeed) && maximumSpeed > 0.0f)
        {
            var maximumPerTick = maximumSpeed * Server.TickInterval;
            extra = Math.Min(extra, Math.Max(0.0f, maximumPerTick - safeMovement));
        }

        return extra;
    }

    private static float DistanceSquared(Vector first, Vector second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        var dz = first.Z - second.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    private static float FiniteOr(float value, float fallback, float minimum, float maximum) =>
        float.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;
}
