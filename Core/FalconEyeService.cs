using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class FalconEyeService
{
    public const string CameraModel = "models/sprays/spray_plane.vmdl";

    private sealed class CameraState
    {
        public required uint PawnIndex { get; set; }
        public required uint OriginalView { get; set; }
        public required CDynamicProp Camera { get; init; }
        public bool Enabled { get; set; }
    }

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly FalconEyeSettings _settings;
    private readonly Dictionary<uint, CameraState> _cameras = new();
    private bool _loaded;

    public FalconEyeService(Myrt1eSkillRemakePlugin plugin, FalconEyeSettings settings)
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
        foreach (var controllerIndex in _cameras.Keys.ToArray())
        {
            Remove(controllerIndex);
        }

        if (_loaded)
        {
            _plugin.RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            _loaded = false;
        }
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
        state.Enabled = !state.Enabled;
        pawn.CameraServices.ViewEntity.Raw = state.Enabled
            ? state.Camera.EntityHandle.Raw
            : state.OriginalView;
        Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

        if (state.Enabled)
        {
            BlockWeapons(player);
        }
        else
        {
            UnblockWeapons(player);
        }

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
            UnblockWeapons(player);
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
            UnblockWeapons(player);
            return;
        }

        PrepareForPawn(state, pawn);
        if (pawn.AbsOrigin is null)
        {
            return;
        }

        var distance = float.IsFinite(_settings.Distance)
            ? Math.Max(0.0f, _settings.Distance)
            : 1000.0f;
        var position = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + distance);
        var angle = new QAngle(90.0f, 0.0f, -pawn.V_angle.Y);
        state.Camera.Teleport(position, angle, null);

        if (state.Enabled)
        {
            BlockWeapons(player);
        }
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
            UnblockWeapons(player!);
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
        state.Enabled = false;
    }

    private static void RestoreView(CCSPlayerPawn pawn, CameraState state)
    {
        if (pawn.CameraServices is null)
        {
            return;
        }

        if (state.Enabled || pawn.CameraServices.ViewEntity.Raw == state.Camera.EntityHandle.Raw)
        {
            pawn.CameraServices.ViewEntity.Raw = state.OriginalView;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
        }

        state.Enabled = false;
    }

    private static void BlockWeapons(CCSPlayerController player)
    {
        SetWeaponAttackTick(player, int.MaxValue);
    }

    private static void UnblockWeapons(CCSPlayerController player)
    {
        SetWeaponAttackTick(player, Server.TickCount);
    }

    private static void SetWeaponAttackTick(CCSPlayerController player, int tick)
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

            weapon.NextPrimaryAttackTick = tick;
            weapon.NextSecondaryAttackTick = tick;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
        }
    }

    private static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(CameraModel);
    }
}
