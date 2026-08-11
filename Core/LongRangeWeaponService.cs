using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using RayTraceAPI;

namespace Myrt1eSkill_Remake.Core;

public sealed class LongRangeWeaponService
{
    private const string CapabilityName = "raytrace:craytraceinterface";

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly PluginCapability<CRayTraceInterface> _capability = new(CapabilityName);
    private bool _missingLogged;

    public LongRangeWeaponService(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
    }

    public bool TryTracePlayer(
        CCSPlayerController shooter,
        float maximumDistance,
        out CCSPlayerController? target)
    {
        target = null;
        var pawn = shooter.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (!shooter.IsValid
            || !shooter.PawnIsAlive
            || pawn is not { IsValid: true }
            || pawn.Handle == nint.Zero
            || pawn.Collision is null
            || origin is null)
        {
            return false;
        }

        CRayTraceInterface? rayTrace;
        try
        {
            rayTrace = _capability.Get();
        }
        catch (Exception exception)
        {
            LogMissing(exception);
            return false;
        }

        if (rayTrace is null)
        {
            LogMissing(null);
            return false;
        }

        var start = new Vector(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
        var forward = Forward(pawn.EyeAngles);
        var distance = float.IsFinite(maximumDistance) && maximumDistance > 0.0f
            ? maximumDistance
            : 4096.0f;
        var end = new Vector(
            start.X + forward.X * distance,
            start.Y + forward.Y * distance,
            start.Z + forward.Z * distance);
        var mask = pawn.Collision.CollisionAttribute.InteractsWith | (ulong)InteractionLayers.Hitboxes;
        mask &= ~(ulong)InteractionLayers.PlayerClip;
        var options = new TraceOptions
        {
            InteractsWith = mask,
            InteractsExclude = 0,
            DrawBeam = 0
        };
        var mins = new Vector(-0.5f, -0.5f, -0.5f);
        var maxs = new Vector(0.5f, 0.5f, 0.5f);
        TraceResult result = default;

        try
        {
            rayTrace.TraceHullShape(start, end, mins, maxs, pawn, options, out result);
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(exception, "Long-range weapon ray trace failed");
            return false;
        }

        if (!result.DidHit)
        {
            return false;
        }

        var hitEntity = new CEntityInstance(result.HitEntity);
        var hitPawn = hitEntity.DesignerName == "player"
            ? hitEntity.As<CCSPlayerPawn>()
            : null;
        target = hitPawn?.Controller.Value?.As<CCSPlayerController>();
        return target is { IsValid: true, PawnIsAlive: true } && target.Slot != shooter.Slot;
    }

    private void LogMissing(Exception? exception)
    {
        if (_missingLogged)
        {
            return;
        }

        _missingLogged = true;
        _plugin.Logger.LogError(
            exception,
            "Ray-Trace capability {Capability} is unavailable; long-range knife and Zeus skills cannot acquire targets",
            CapabilityName);
    }

    private static Vector Forward(QAngle angle)
    {
        var pitch = angle.X * MathF.PI / 180.0f;
        var yaw = angle.Y * MathF.PI / 180.0f;
        var cosinePitch = MathF.Cos(pitch);
        return new Vector(cosinePitch * MathF.Cos(yaw), cosinePitch * MathF.Sin(yaw), -MathF.Sin(pitch));
    }
}
