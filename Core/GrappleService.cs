using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Myrt1eSkill_Remake.Configuration;
using RayTraceAPI;

namespace Myrt1eSkill_Remake.Core;

public sealed class GrappleService
{
    public const string HookModel = "models/generic/grapplinghook_01/grapplinghook_hook_01_open.vmdl";
    private const string CapabilityName = "raytrace:craytraceinterface";

    public readonly record struct TraceHit(Vector Position, Vector Normal);

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly GrappleSettings _settings;
    private readonly PluginCapability<CRayTraceInterface> _capability = new(CapabilityName);
    private bool _missingLogged;
    private bool _loaded;

    public GrappleService(Myrt1eSkillRemakePlugin plugin, GrappleSettings settings)
    {
        _plugin = plugin;
        _settings = settings;
    }

    public void Load()
    {
        if (_loaded)
        {
            return;
        }

        _plugin.RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        _loaded = true;
    }

    public void Unload()
    {
        if (!_loaded)
        {
            return;
        }

        _plugin.RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        _loaded = false;
    }

    public bool TryEyeTrace(CCSPlayerController player, out TraceHit hit)
    {
        hit = default;
        var pawn = player.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (!player.IsValid
            || !player.PawnIsAlive
            || pawn is not { IsValid: true }
            || pawn.Handle == IntPtr.Zero
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
        var distance = FinitePositiveOr(_settings.MaximumDistance, 1500.0f);
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
            _plugin.Logger.LogError(exception, "Grapple ray trace failed");
            return false;
        }

        if (!result.DidHit)
        {
            return false;
        }

        var hitEntity = new CEntityInstance(result.HitEntity);
        if (string.Equals(hitEntity.DesignerName, "player", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        hit = new TraceHit(
            new Vector(result.EndPos.X, result.EndPos.Y, result.EndPos.Z),
            new Vector(result.Normal.X, result.Normal.Y, result.Normal.Z));
        return true;
    }

    private void OnServerPrecacheResources(ResourceManifest manifest) => manifest.AddResource(HookModel);

    private void LogMissing(Exception? exception)
    {
        if (_missingLogged)
        {
            return;
        }

        _missingLogged = true;
        _plugin.Logger.LogError(
            exception,
            "Ray-Trace capability {Capability} is unavailable; Grapple cannot acquire anchors",
            CapabilityName);
    }

    private static Vector Forward(QAngle angle)
    {
        var pitch = angle.X * MathF.PI / 180.0f;
        var yaw = angle.Y * MathF.PI / 180.0f;
        var cosinePitch = MathF.Cos(pitch);
        return new Vector(cosinePitch * MathF.Cos(yaw), cosinePitch * MathF.Sin(yaw), -MathF.Sin(pitch));
    }

    private static float FinitePositiveOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;
}
