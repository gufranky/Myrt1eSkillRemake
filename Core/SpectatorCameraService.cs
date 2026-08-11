using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public enum SpectatorToggleResult
{
    Failed,
    Started,
    Stopped
}

public sealed class SpectatorCameraService
{
    public const string CameraModel = "models/sprays/spray_plane.vmdl";

    private sealed class SpectatorCameraState
    {
        public required uint ViewerPawnIndex { get; set; }
        public required uint OriginalView { get; set; }
        public required uint TargetIndex { get; init; }
        public required CDynamicProp Camera { get; init; }
    }

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly SpectatorSettings _settings;
    private readonly Dictionary<uint, SpectatorCameraState> _cameras = new();
    private bool _loaded;

    public SpectatorCameraService(
        Myrt1eSkillRemakePlugin plugin,
        SpectatorSettings settings)
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
        foreach (var viewerIndex in _cameras.Keys.ToArray())
        {
            Remove(viewerIndex);
        }

        if (_loaded)
        {
            _plugin.RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            _loaded = false;
        }
    }

    public SpectatorToggleResult Toggle(
        CCSPlayerController viewer,
        out CCSPlayerController? target)
    {
        target = null;
        if (!viewer.IsValid || !viewer.PawnIsAlive)
        {
            return SpectatorToggleResult.Failed;
        }

        if (_cameras.ContainsKey(viewer.Index))
        {
            Remove(viewer.Index, viewer);
            return SpectatorToggleResult.Stopped;
        }

        var enemies = Utilities.GetPlayers()
            .Where(player => player.IsValid
                             && player.PawnIsAlive
                             && player.Team != viewer.Team
                             && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            .ToArray();
        if (enemies.Length == 0)
        {
            return SpectatorToggleResult.Failed;
        }

        target = enemies[Random.Shared.Next(enemies.Length)];
        return TryCreate(viewer, target)
            ? SpectatorToggleResult.Started
            : SpectatorToggleResult.Failed;
    }

    public void Update(CCSPlayerController viewer)
    {
        if (!_cameras.TryGetValue(viewer.Index, out var state))
        {
            return;
        }

        var viewerPawn = viewer.PlayerPawn.Value;
        var target = Utilities.GetPlayerFromIndex((int)state.TargetIndex);
        var targetPawn = target?.PlayerPawn.Value;
        if (!viewer.IsValid
            || !viewer.PawnIsAlive
            || viewerPawn is not { IsValid: true }
            || viewerPawn.CameraServices is null
            || target is not { IsValid: true, PawnIsAlive: true }
            || targetPawn is not { IsValid: true }
            || targetPawn.AbsOrigin is null
            || !state.Camera.IsValid)
        {
            Remove(viewer.Index, viewer);
            return;
        }

        if (state.ViewerPawnIndex != viewerPawn.Index)
        {
            RestoreView(viewerPawn, state);
            Remove(viewer.Index, viewer);
            return;
        }

        var distance = float.IsFinite(_settings.Distance)
            ? Math.Max(0.0f, _settings.Distance)
            : 100.0f;
        var angle = new QAngle(0.0f, targetPawn.EyeAngles.Y, 0.0f);
        var forward = GetForwardVector(angle);
        var position = new Vector(
            targetPawn.AbsOrigin.X - forward.X * distance,
            targetPawn.AbsOrigin.Y - forward.Y * distance,
            targetPawn.AbsOrigin.Z + targetPawn.ViewOffset.Z);
        state.Camera.Teleport(position, angle, null);
        BlockWeapons(viewer);
    }

    public void Remove(uint viewerIndex, CCSPlayerController? viewer = null)
    {
        if (!_cameras.Remove(viewerIndex, out var state))
        {
            return;
        }

        viewer ??= Utilities.GetPlayerFromIndex((int)viewerIndex);
        var pawn = viewer?.PlayerPawn.Value;
        if (pawn is { IsValid: true })
        {
            RestoreView(pawn, state);
            UnblockWeapons(viewer!);
        }

        if (state.Camera.IsValid)
        {
            state.Camera.Remove();
        }
    }

    private bool TryCreate(CCSPlayerController viewer, CCSPlayerController target)
    {
        var viewerPawn = viewer.PlayerPawn.Value;
        var targetPawn = target.PlayerPawn.Value;
        if (viewerPawn is not { IsValid: true }
            || viewerPawn.CameraServices is null
            || targetPawn is not { IsValid: true }
            || targetPawn.AbsOrigin is null)
        {
            return false;
        }

        var camera = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (camera is null || !camera.IsValid)
        {
            return false;
        }

        try
        {
            var sceneOwner = camera.CBodyComponent?.SceneNode?.Owner?.Entity;
            if (sceneOwner is not null)
            {
                sceneOwner.Flags &= ~(uint)(1 << 2);
            }

            var distance = float.IsFinite(_settings.Distance)
                ? Math.Max(0.0f, _settings.Distance)
                : 100.0f;
            var angle = new QAngle(0.0f, targetPawn.EyeAngles.Y, 0.0f);
            var forward = GetForwardVector(angle);
            var position = new Vector(
                targetPawn.AbsOrigin.X - forward.X * distance,
                targetPawn.AbsOrigin.Y - forward.Y * distance,
                targetPawn.AbsOrigin.Z + targetPawn.ViewOffset.Z);
            camera.SetModel(CameraModel);
            camera.Render = Color.FromArgb(0, 255, 255, 255);
            camera.Teleport(position, angle, null);
            camera.DispatchSpawn();

            var state = new SpectatorCameraState
            {
                ViewerPawnIndex = viewerPawn.Index,
                OriginalView = viewerPawn.CameraServices.ViewEntity.Raw,
                TargetIndex = target.Index,
                Camera = camera
            };
            _cameras[viewer.Index] = state;
            viewerPawn.CameraServices.ViewEntity.Raw = camera.EntityHandle.Raw;
            Utilities.SetStateChanged(viewerPawn, "CBasePlayerPawn", "m_pCameraServices");
            BlockWeapons(viewer);
            return true;
        }
        catch
        {
            if (camera.IsValid)
            {
                camera.Remove();
            }

            throw;
        }
    }

    private static void RestoreView(
        CCSPlayerPawn pawn,
        SpectatorCameraState state)
    {
        if (pawn.CameraServices is null)
        {
            return;
        }

        if (pawn.CameraServices.ViewEntity.Raw == state.Camera.EntityHandle.Raw)
        {
            pawn.CameraServices.ViewEntity.Raw = state.OriginalView;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
        }
    }

    private static void BlockWeapons(CCSPlayerController player)
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

            weapon.NextPrimaryAttackTick = int.MaxValue;
            weapon.NextSecondaryAttackTick = int.MaxValue;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
        }
    }

    private static void UnblockWeapons(CCSPlayerController player)
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

            weapon.NextPrimaryAttackTick = Server.TickCount;
            weapon.NextSecondaryAttackTick = Server.TickCount;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
        }
    }

    private static Vector GetForwardVector(QAngle angles)
    {
        var yaw = angles.Y * (MathF.PI / 180.0f);
        return new Vector(MathF.Cos(yaw), MathF.Sin(yaw), 0.0f);
    }

    private static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(CameraModel);
    }
}
