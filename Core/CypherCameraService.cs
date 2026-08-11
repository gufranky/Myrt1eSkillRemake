using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Myrt1eSkill_Remake.Configuration;
using RayTraceAPI;

namespace Myrt1eSkill_Remake.Core;

public enum CypherToggleResult
{
    Failed,
    NoSurface,
    Cooldown,
    Deployed,
    Entered,
    Exited
}

public sealed class CypherCameraService
{
    public const string CameraPropModel = "models/props/de_train/hr_train_s2/train_electronics/train_electronics_security_camera_01.vmdl";
    public const string CameraViewModel = "models/sprays/spray_plane.vmdl";
    private const string CameraNamePrefix = "myrt1eskill_cypher_camera_";
    private const string RayTraceCapability = "raytrace:craytraceinterface";

    private sealed class CameraState
    {
        public required uint OwnerIndex { get; init; }
        public uint PawnIndex { get; set; }
        public uint OriginalView { get; set; }
        public uint? CameraPropIndex { get; set; }
        public uint? CameraViewIndex { get; set; }
        public bool Active { get; set; }
        public int NextDeployTick { get; set; }
        public int NextOverlayTick { get; set; }
        public QAngle LastPlayerAngle { get; set; } = QAngle.Zero;
        public QAngle LastCameraAngle { get; set; } = QAngle.Zero;
    }

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly CypherSettings _settings;
    private readonly PlayerViewService _playerView;
    private readonly PluginCapability<CRayTraceInterface> _rayTrace = new(RayTraceCapability);
    private readonly Dictionary<uint, CameraState> _states = new();
    private readonly Dictionary<uint, uint> _ownersByProp = new();
    private bool _missingRayTraceLogged;
    private bool _loaded;

    public CypherCameraService(
        Myrt1eSkillRemakePlugin plugin,
        CypherSettings settings,
        PlayerViewService playerView)
    {
        _plugin = plugin;
        _settings = settings;
        _playerView = playerView;
    }

    public void Load()
    {
        if (_loaded)
        {
            return;
        }

        _plugin.RegisterListener<Listeners.OnEntityTakeDamagePre>(OnEntityTakeDamage);
        _plugin.RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        _loaded = true;
    }

    public void Unload()
    {
        foreach (var ownerIndex in _states.Keys.ToArray())
        {
            Remove(ownerIndex);
        }

        if (_loaded)
        {
            _plugin.RemoveListener<Listeners.OnEntityTakeDamagePre>(OnEntityTakeDamage);
            _plugin.RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            _loaded = false;
        }
    }

    public CypherToggleResult Toggle(CCSPlayerController player, out float cooldownRemaining)
    {
        cooldownRemaining = 0.0f;
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return CypherToggleResult.Failed;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true } || pawn.CameraServices is null || pawn.AbsOrigin is null)
        {
            return CypherToggleResult.Failed;
        }

        var state = GetOrCreateState(player, pawn);
        PrepareForPawn(state, pawn);
        var prop = GetProp(state);
        var view = GetView(state);
        if (state.CameraPropIndex.HasValue && prop is not { IsValid: true })
        {
            DestroyCamera(state, player, startCooldown: true, notify: false);
            prop = null;
            view = null;
        }

        if (prop is null || view is not { IsValid: true })
        {
            if (state.NextDeployTick > Server.TickCount)
            {
                cooldownRemaining = (state.NextDeployTick - Server.TickCount) / 64.0f;
                return CypherToggleResult.Cooldown;
            }

            DestroyCamera(state, player, startCooldown: false, notify: false);
            if (!TryDeploy(player, pawn, state))
            {
                return CypherToggleResult.NoSurface;
            }

            SetActive(player, pawn, state, true);
            return CypherToggleResult.Deployed;
        }

        SetActive(player, pawn, state, !state.Active);
        return state.Active ? CypherToggleResult.Entered : CypherToggleResult.Exited;
    }

    public void Update(CCSPlayerController player)
    {
        if (!_states.TryGetValue(player.Index, out var state))
        {
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (!player.IsValid
            || !player.PawnIsAlive
            || pawn is not { IsValid: true }
            || pawn.CameraServices is null)
        {
            ExitView(player, pawn, state);
            return;
        }

        PrepareForPawn(state, pawn);
        var prop = GetProp(state);
        var view = GetView(state);
        if (prop is not { IsValid: true } || view is not { IsValid: true })
        {
            DestroyCamera(state, player, startCooldown: true, notify: false);
            return;
        }

        if (!state.Active)
        {
            return;
        }

        state.LastCameraAngle = new QAngle(pawn.V_angle.X, pawn.V_angle.Y, 0.0f);
        view.Teleport(null, pawn.V_angle, null);
        BlockWeapons(player, true);
        if (Server.TickCount >= state.NextOverlayTick)
        {
            SendOverlay(player, clear: false);
            state.NextOverlayTick = Server.TickCount + 128;
        }
    }

    public void Remove(uint ownerIndex, CCSPlayerController? player = null)
    {
        if (!_states.Remove(ownerIndex, out var state))
        {
            return;
        }

        player ??= Utilities.GetPlayerFromIndex((int)ownerIndex);
        DestroyCamera(state, player, startCooldown: false, notify: false);
    }

    private CameraState GetOrCreateState(CCSPlayerController player, CCSPlayerPawn pawn)
    {
        if (_states.TryGetValue(player.Index, out var state))
        {
            return state;
        }

        state = new CameraState
        {
            OwnerIndex = player.Index,
            PawnIndex = pawn.Index,
            OriginalView = pawn.CameraServices?.ViewEntity.Raw ?? 0
        };
        _states[player.Index] = state;
        return state;
    }

    private bool TryDeploy(CCSPlayerController player, CCSPlayerPawn pawn, CameraState state)
    {
        if (!TryTraceSurface(player, out var hitPosition, out var hitNormal))
        {
            return false;
        }

        var normalLength = MathF.Sqrt(
            hitNormal.X * hitNormal.X + hitNormal.Y * hitNormal.Y + hitNormal.Z * hitNormal.Z);
        if (normalLength < 0.0001f)
        {
            return false;
        }

        var offset = FiniteOr(_settings.SurfaceOffset, 8.0f);
        var propPosition = new Vector(
            hitPosition.X + hitNormal.X / normalLength * offset,
            hitPosition.Y + hitNormal.Y / normalLength * offset,
            hitPosition.Z + hitNormal.Z / normalLength * offset);
        var propAngle = new QAngle(0.0f, pawn.V_angle.Y + 180.0f, 0.0f);
        var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (prop is not { IsValid: true })
        {
            return false;
        }

        CDynamicProp? view = null;
        try
        {
            prop.Entity!.Name = $"{CameraNamePrefix}{player.Index}_{prop.Index}";
            prop.Globalname = prop.Entity.Name;
            prop.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            prop.Health = prop.MaxHealth = 1_000_000;
            var propSceneOwner = prop.CBodyComponent?.SceneNode?.Owner?.Entity;
            if (propSceneOwner is not null)
            {
                propSceneOwner.Flags &= ~(uint)(1 << 2);
            }
            prop.SetModel(CameraPropModel);
            prop.Teleport(propPosition, propAngle, null);
            prop.DispatchSpawn();

            var forward = Forward(propAngle);
            var viewOffset = FiniteOr(_settings.ViewOffset, 25.0f);
            var viewPosition = new Vector(
                propPosition.X + forward.X * viewOffset,
                propPosition.Y + forward.Y * viewOffset,
                propPosition.Z + forward.Z * viewOffset);
            view = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (view is not { IsValid: true })
            {
                prop.Remove();
                return false;
            }

            var viewSceneOwner = view.CBodyComponent?.SceneNode?.Owner?.Entity;
            if (viewSceneOwner is not null)
            {
                viewSceneOwner.Flags &= ~(uint)(1 << 2);
            }
            view.SetModel(CameraViewModel);
            view.Render = Color.FromArgb(1, 255, 255, 255);
            view.Teleport(viewPosition, propAngle, null);
            view.DispatchSpawn();

            state.CameraPropIndex = prop.Index;
            state.CameraViewIndex = view.Index;
            state.LastCameraAngle = propAngle;
            _ownersByProp[prop.Index] = player.Index;
            return true;
        }
        catch
        {
            if (view is { IsValid: true })
            {
                view.Remove();
            }
            if (prop.IsValid)
            {
                prop.Remove();
            }
            throw;
        }
    }

    private void SetActive(
        CCSPlayerController player,
        CCSPlayerPawn pawn,
        CameraState state,
        bool active)
    {
        if (pawn.CameraServices is null)
        {
            return;
        }

        var view = GetView(state);
        if (active && view is not { IsValid: true })
        {
            return;
        }

        if (active)
        {
            state.LastPlayerAngle = new QAngle(pawn.V_angle.X, pawn.V_angle.Y, 0.0f);
            if (state.LastCameraAngle != QAngle.Zero)
            {
                _playerView.TrySet(pawn, state.LastCameraAngle);
            }
            pawn.CameraServices.ViewEntity.Raw = view!.EntityHandle.Raw;
            state.Active = true;
            state.NextOverlayTick = 0;
            BlockWeapons(player, true);
            SendOverlay(player, clear: false);
        }
        else
        {
            state.LastCameraAngle = new QAngle(pawn.V_angle.X, pawn.V_angle.Y, 0.0f);
            pawn.CameraServices.ViewEntity.Raw = state.OriginalView;
            state.Active = false;
            _playerView.TrySet(pawn, state.LastPlayerAngle);
            BlockWeapons(player, false);
            SendOverlay(player, clear: true);
        }

        Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
    }

    private void ExitView(CCSPlayerController? player, CCSPlayerPawn? pawn, CameraState state)
    {
        if (!state.Active)
        {
            return;
        }

        if (pawn is { IsValid: true } && pawn.CameraServices is not null)
        {
            pawn.CameraServices.ViewEntity.Raw = state.OriginalView;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
        }

        state.Active = false;
        if (player is { IsValid: true })
        {
            BlockWeapons(player, false);
            SendOverlay(player, clear: true);
        }
    }

    private void DestroyCamera(
        CameraState state,
        CCSPlayerController? player,
        bool startCooldown,
        bool notify)
    {
        var pawn = player?.PlayerPawn.Value;
        ExitView(player, pawn, state);

        if (state.CameraPropIndex is { } propIndex)
        {
            _ownersByProp.Remove(propIndex);
            var prop = Utilities.GetEntityFromIndex<CDynamicProp>((int)propIndex);
            if (prop is { IsValid: true })
            {
                prop.EmitSound(
                    "SolidMetal.BulletImpact",
                    volume: Math.Clamp(FiniteOr(_settings.SoundVolume, 1.0f), 0.0f, 1.0f));
                prop.Remove();
            }
        }

        if (state.CameraViewIndex is { } viewIndex)
        {
            var view = Utilities.GetEntityFromIndex<CDynamicProp>((int)viewIndex);
            if (view is { IsValid: true })
            {
                view.Remove();
            }
        }

        state.CameraPropIndex = null;
        state.CameraViewIndex = null;
        if (startCooldown)
        {
            state.NextDeployTick = Server.TickCount + SecondsToTicks(_settings.DeployCooldownSeconds, 30.0f);
        }
        if (notify && player is { IsValid: true })
        {
            var cooldown = Math.Ceiling(PositiveFiniteOr(_settings.DeployCooldownSeconds, 30.0f));
            PluginText.Chat(player, $"[Cypher] 摄像头已被摧毁，{cooldown:0} 秒后可以重新部署。");
        }
    }

    private HookResult OnEntityTakeDamage(CEntityInstance entity, CTakeDamageInfo damageInfo)
    {
        if (entity is null
            || !entity.IsValid
            || !_ownersByProp.TryGetValue(entity.Index, out var ownerIndex)
            || !_states.TryGetValue(ownerIndex, out var state))
        {
            return HookResult.Continue;
        }

        var owner = Utilities.GetPlayerFromIndex((int)ownerIndex);
        DestroyCamera(state, owner, startCooldown: true, notify: true);
        return HookResult.Continue;
    }

    private bool TryTraceSurface(
        CCSPlayerController player,
        out Vector position,
        out Vector normal)
    {
        position = Vector.Zero;
        normal = Vector.Zero;
        var pawn = player.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (pawn is not { IsValid: true }
            || pawn.Handle == IntPtr.Zero
            || pawn.Collision is null
            || origin is null)
        {
            return false;
        }

        CRayTraceInterface? rayTrace;
        try
        {
            rayTrace = _rayTrace.Get();
        }
        catch (Exception exception)
        {
            LogMissingRayTrace(exception);
            return false;
        }

        if (rayTrace is null)
        {
            LogMissingRayTrace(null);
            return false;
        }

        var start = new Vector(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
        var forward = Forward(pawn.EyeAngles);
        var distance = PositiveFiniteOr(_settings.MaximumDistance, 4096.0f);
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
            _plugin.Logger.LogError(exception, "Cypher camera ray trace failed");
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

        position = new Vector(result.EndPos.X, result.EndPos.Y, result.EndPos.Z);
        normal = new Vector(result.Normal.X, result.Normal.Y, result.Normal.Z);
        return true;
    }

    private static void PrepareForPawn(CameraState state, CCSPlayerPawn pawn)
    {
        if (state.PawnIndex == pawn.Index || pawn.CameraServices is null)
        {
            return;
        }

        state.PawnIndex = pawn.Index;
        state.OriginalView = pawn.CameraServices.ViewEntity.Raw;
        state.Active = false;
    }

    private static CDynamicProp? GetProp(CameraState state) =>
        state.CameraPropIndex is { } index
            ? Utilities.GetEntityFromIndex<CDynamicProp>((int)index)
            : null;

    private static CDynamicProp? GetView(CameraState state) =>
        state.CameraViewIndex is { } index
            ? Utilities.GetEntityFromIndex<CDynamicProp>((int)index)
            : null;

    private static void BlockWeapons(CCSPlayerController player, bool block)
    {
        var weapons = player.PlayerPawn.Value?.WeaponServices?.MyWeapons;
        if (weapons is null)
        {
            return;
        }

        foreach (var handle in weapons)
        {
            var weapon = handle.Value;
            if (weapon is not { IsValid: true })
            {
                continue;
            }

            var tick = block ? int.MaxValue : Server.TickCount;
            weapon.NextPrimaryAttackTick = tick;
            weapon.NextSecondaryAttackTick = tick;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
        }
    }

    private static void SendOverlay(CCSPlayerController player, bool clear)
    {
        using var message = UserMessage.FromPartialName("Fade");
        if (message is null)
        {
            return;
        }

        message.SetInt("duration", clear ? 200 : 100);
        message.SetInt("hold_time", clear ? 0 : 1020);
        message.SetInt("flags", 1);
        message.SetInt("color", clear ? 0 : DarknessService.PackColor(0, 0, 255, 20));
        message.Send(player);
    }

    private void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(CameraPropModel);
        manifest.AddResource(CameraViewModel);
    }

    private void LogMissingRayTrace(Exception? exception)
    {
        if (_missingRayTraceLogged)
        {
            return;
        }

        _missingRayTraceLogged = true;
        _plugin.Logger.LogError(
            exception,
            "Ray-Trace capability {Capability} is unavailable; Cypher cannot deploy cameras",
            RayTraceCapability);
    }

    private static Vector Forward(QAngle angle)
    {
        var pitch = angle.X * MathF.PI / 180.0f;
        var yaw = angle.Y * MathF.PI / 180.0f;
        var cosinePitch = MathF.Cos(pitch);
        return new Vector(cosinePitch * MathF.Cos(yaw), cosinePitch * MathF.Sin(yaw), -MathF.Sin(pitch));
    }

    private static int SecondsToTicks(float seconds, float fallback) =>
        Math.Max(0, (int)MathF.Ceiling(PositiveFiniteOr(seconds, fallback) * 64.0f));

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static float FiniteOr(float value, float fallback) =>
        float.IsFinite(value) ? value : fallback;
}
