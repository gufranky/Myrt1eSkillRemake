using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class ThirdEyeService
{
    private const string CameraModel = "models/sprays/spray_plane.vmdl";

    private sealed class CameraState
    {
        public required uint PawnIndex { get; set; }
        public required uint OriginalView { get; set; }
        public required CDynamicProp Camera { get; init; }
        public bool ThirdPersonEnabled { get; set; }
    }

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly ThirdEyeSettings _settings;
    private readonly Dictionary<uint, CameraState> _cameras = new();
    private bool _loaded;

    public ThirdEyeService(Myrt1eSkillRemakePlugin plugin, ThirdEyeSettings settings)
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

        foreach (var controllerIndex in _cameras.Keys.ToArray())
        {
            Remove(controllerIndex, Utilities.GetPlayerFromIndex((int)controllerIndex));
        }

        _plugin.RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        _loaded = false;
    }

    public bool Toggle(CCSPlayerController player)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true } || pawn.CameraServices is null || pawn.AbsOrigin is null)
        {
            return false;
        }

        if (!_cameras.TryGetValue(player.Index, out var state) || !state.Camera.IsValid)
        {
            Remove(player.Index, player);
            state = CreateCamera(player, pawn);
            if (state is null)
            {
                return false;
            }
        }

        PrepareForPawn(state, pawn);
        state.ThirdPersonEnabled = !state.ThirdPersonEnabled;
        pawn.CameraServices.ViewEntity.Raw = state.ThirdPersonEnabled
            ? state.Camera.EntityHandle.Raw
            : state.OriginalView;
        Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
        return true;
    }

    public void Update(CCSPlayerController player)
    {
        if (!_cameras.TryGetValue(player.Index, out var state))
        {
            return;
        }

        if (!state.Camera.IsValid)
        {
            _cameras.Remove(player.Index);
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (!player.IsValid || pawn is not { IsValid: true } || pawn.CameraServices is null)
        {
            return;
        }

        if (!player.PawnIsAlive || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            RestoreView(pawn, state);
            return;
        }

        PrepareForPawn(state, pawn);
        if (pawn.AbsOrigin is null)
        {
            return;
        }

        var distance = float.IsFinite(_settings.Distance)
            ? Math.Max(0.0f, _settings.Distance)
            : 100.0f;
        var forward = GetForwardVector(pawn.EyeAngles);
        var position = new Vector(
            pawn.AbsOrigin.X - forward.X * distance,
            pawn.AbsOrigin.Y - forward.Y * distance,
            pawn.AbsOrigin.Z + pawn.ViewOffset.Z - forward.Z * distance);
        state.Camera.Teleport(position, pawn.V_angle, null);
    }

    public void Remove(uint controllerIndex, CCSPlayerController? player = null)
    {
        if (!_cameras.Remove(controllerIndex, out var state))
        {
            return;
        }

        player ??= Utilities.GetPlayerFromIndex((int)controllerIndex);
        var pawn = player?.PlayerPawn.Value;
        if (pawn is { IsValid: true } && pawn.CameraServices is not null)
        {
            RestoreView(pawn, state);
        }

        if (state.Camera.IsValid)
        {
            state.Camera.Remove();
        }
    }

    private CameraState? CreateCamera(CCSPlayerController player, CCSPlayerPawn pawn)
    {
        var camera = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (camera is null || !camera.IsValid || pawn.CameraServices is null)
        {
            return null;
        }

        var sceneOwner = camera.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (sceneOwner is not null)
        {
            sceneOwner.Flags &= ~(uint)(1 << 2);
        }

        camera.SetModel(CameraModel);
        camera.Render = Color.FromArgb(0, 255, 255, 255);
        camera.Teleport(pawn.AbsOrigin, pawn.EyeAngles, null);
        camera.DispatchSpawn();

        var state = new CameraState
        {
            PawnIndex = pawn.Index,
            OriginalView = pawn.CameraServices.ViewEntity.Raw,
            Camera = camera
        };
        _cameras[player.Index] = state;
        return state;
    }

    private static void PrepareForPawn(CameraState state, CCSPlayerPawn pawn)
    {
        if (state.PawnIndex == pawn.Index || pawn.CameraServices is null)
        {
            return;
        }

        state.PawnIndex = pawn.Index;
        state.OriginalView = pawn.CameraServices.ViewEntity.Raw;
        state.ThirdPersonEnabled = false;
    }

    private static void RestoreView(CCSPlayerPawn pawn, CameraState state)
    {
        if (pawn.CameraServices is null)
        {
            return;
        }

        if (state.ThirdPersonEnabled
            || pawn.CameraServices.ViewEntity.Raw == state.Camera.EntityHandle.Raw)
        {
            pawn.CameraServices.ViewEntity.Raw = state.OriginalView;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
        }

        state.ThirdPersonEnabled = false;
    }

    private static Vector GetForwardVector(QAngle angles)
    {
        var pitch = -angles.X * (MathF.PI / 180.0f);
        var yaw = angles.Y * (MathF.PI / 180.0f);
        return new Vector(
            MathF.Cos(pitch) * MathF.Cos(yaw),
            MathF.Cos(pitch) * MathF.Sin(yaw),
            MathF.Sin(pitch));
    }

    private static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(CameraModel);
    }
}
