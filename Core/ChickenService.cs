using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class ChickenService : IDisposable
{
    private const string ChickenModel = "models/chicken/chicken.vmdl";

    private sealed class ChickenState
    {
        public required CCSPlayerPawn Pawn { get; init; }
        public required CBaseModelEntity Model { get; init; }
        public required Color Render { get; init; }
        public required float ShadowStrength { get; init; }
        public required float Scale { get; init; }
        public required float VelocityModifier { get; init; }
        public required int AppliedHealthPenalty { get; init; }
    }

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly ChickenSettings _settings;
    private readonly Dictionary<uint, ChickenState> _states = new();
    private bool _loaded;

    public ChickenService(Myrt1eSkillRemakePlugin plugin, ChickenSettings settings)
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

    public bool Apply(CCSPlayerController player)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return false;
        }

        Remove(player.Index, player);
        var model = CreateModel(pawn);
        if (model is null)
        {
            return false;
        }

        var healthPenalty = Math.Min(Math.Max(0, _settings.HealthPenalty), Math.Max(0, pawn.Health - 1));
        _states[player.Index] = new ChickenState
        {
            Pawn = pawn,
            Model = model,
            Render = pawn.Render,
            ShadowStrength = pawn.ShadowStrength,
            Scale = GetScale(pawn),
            VelocityModifier = pawn.VelocityModifier,
            AppliedHealthPenalty = healthPenalty
        };

        pawn.Render = Color.FromArgb(0, 255, 255, 255);
        pawn.ShadowStrength = 0.0f;
        SetScale(pawn, Math.Max(0.01f, _settings.PlayerScale));
        pawn.VelocityModifier = Math.Max(0.01f, _settings.SpeedMultiplier);
        pawn.Health -= healthPenalty;
        MarkPawnChanged(pawn);
        return true;
    }

    public void Update(CCSPlayerController player)
    {
        if (_states.TryGetValue(player.Index, out var state)
            && state.Pawn.IsValid
            && state.Pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
        {
            state.Pawn.VelocityModifier = Math.Max(0.01f, _settings.SpeedMultiplier);
            Utilities.SetStateChanged(state.Pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }
    }

    public void Remove(uint controllerIndex, CCSPlayerController? player = null, bool restoreHealth = true)
    {
        if (!_states.Remove(controllerIndex, out var state))
        {
            return;
        }

        if (state.Model.IsValid)
        {
            state.Model.Remove();
        }

        var pawn = state.Pawn;
        if (!pawn.IsValid)
        {
            return;
        }

        pawn.Render = state.Render;
        pawn.ShadowStrength = state.ShadowStrength;
        SetScale(pawn, state.Scale);
        pawn.VelocityModifier = state.VelocityModifier;
        if (restoreHealth && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
        {
            pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + state.AppliedHealthPenalty);
        }

        MarkPawnChanged(pawn);
    }

    public void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_states.Count == 0)
        {
            return;
        }

        foreach (var (info, viewer) in infoList)
        {
            if (viewer is not { IsValid: true })
            {
                continue;
            }

            foreach (var (controllerIndex, state) in _states)
            {
                if (viewer.Index == controllerIndex || !state.Pawn.IsValid)
                {
                    continue;
                }

                // Keep the parent Pawn in the snapshot. The chicken model is
                // parented to it; removing the parent makes the client stop
                // receiving transform updates and leaves the model behind.
                HideWeapons(info, state.Pawn);
            }
        }
    }

    public void Dispose()
    {
        foreach (var controllerIndex in _states.Keys.ToArray())
        {
            Remove(controllerIndex);
        }

        if (_loaded)
        {
            _plugin.RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            _loaded = false;
        }
    }

    private CBaseModelEntity? CreateModel(CCSPlayerPawn pawn)
    {
        var model = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (model is null)
        {
            return null;
        }

        var ownerEntity = model.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (ownerEntity is not null)
        {
            ownerEntity.Flags &= ~(uint)(1 << 2);
        }

        model.SetModel(ChickenModel);
        model.Render = Color.FromArgb(255, 255, 255, 255);
        model.Teleport(pawn.AbsOrigin, pawn.AbsRotation, null);
        model.DispatchSpawn();
        model.AcceptInput("InitializeSpawnFromWorld", pawn, pawn);
        Utilities.SetStateChanged(model, "CBaseEntity", "m_CBodyComponent");
        model.AcceptInput("SetParent", pawn, pawn, "!activator");
        Utilities.SetStateChanged(model, "CBaseEntity", "m_CBodyComponent");
        _plugin.AddTimer(0.01f, () =>
        {
            if (!model.IsValid)
            {
                return;
            }

            model.AcceptInput("SetScale", model, model, "1");
            Utilities.SetStateChanged(model, "CBaseEntity", "m_CBodyComponent");
        });
        return model;
    }

    private static void HideWeapons(CCheckTransmitInfo info, CCSPlayerPawn pawn)
    {
        var weapons = pawn.WeaponServices;
        if (weapons is null)
        {
            return;
        }

        foreach (var weaponHandle in weapons.MyWeapons)
        {
            var weapon = weaponHandle.Value;
            if (weapon is { IsValid: true })
            {
                info.TransmitEntities.Remove(weapon.Index);
            }
        }
    }

    private static float GetScale(CCSPlayerPawn pawn) =>
        pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.Scale ?? 1.0f;

    private static void SetScale(CCSPlayerPawn pawn, float scale)
    {
        var skeleton = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton is null)
        {
            return;
        }

        skeleton.Scale = scale;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
        pawn.AcceptInput("SetScale", pawn, pawn, scale.ToString(CultureInfo.InvariantCulture));
    }

    private static void MarkPawnChanged(CCSPlayerPawn pawn)
    {
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_flShadowStrength");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }

    private static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(ChickenModel);
    }
}
