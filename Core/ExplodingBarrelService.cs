using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class ExplodingBarrelService
{
    private sealed record BarrelSource(uint OwnerIndex);

    private const string BarrelNamePrefix = "myrt1eskill_barrel_";

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly ExplodingBarrelSettings _settings;
    private readonly ExplosiveProjectileService _explosions;
    private readonly ConcurrentDictionary<uint, BarrelSource> _barrels = new();
    private bool _loaded;

    public ExplodingBarrelService(
        Myrt1eSkillRemakePlugin plugin,
        ExplodingBarrelSettings settings,
        ExplosiveProjectileService explosions)
    {
        _plugin = plugin;
        _settings = settings;
        _explosions = explosions;
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
        if (!_loaded)
        {
            return;
        }

        _plugin.RemoveListener<Listeners.OnEntityTakeDamagePre>(OnEntityTakeDamage);
        _plugin.RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        _barrels.Clear();
        _loaded = false;
    }

    public bool Place(CCSPlayerController owner, EffectScope effects)
    {
        if (!owner.IsValid || !owner.PawnIsAlive)
        {
            return false;
        }

        var pawn = owner.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (pawn is null || !pawn.IsValid || origin is null)
        {
            return false;
        }

        var barrel = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (barrel is null || !barrel.IsValid)
        {
            return false;
        }

        var placementDistance = float.IsFinite(_settings.PlacementDistance)
            ? Math.Max(0.0f, _settings.PlacementDistance)
            : 50.0f;
        var yaw = pawn.EyeAngles.Y * (MathF.PI / 180.0f);
        var position = new Vector(
            origin.X + MathF.Cos(yaw) * placementDistance,
            origin.Y + MathF.Sin(yaw) * placementDistance,
            origin.Z);
        var rotation = new QAngle(0.0f, pawn.EyeAngles.Y, 0.0f);

        barrel.Entity!.Name = $"{BarrelNamePrefix}{owner.Index}_{barrel.Index}";
        barrel.Globalname = barrel.Entity.Name;
        barrel.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        var sceneOwner = barrel.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (sceneOwner is not null)
        {
            sceneOwner.Flags &= ~(uint)(1 << 2);
        }

        barrel.SetModel(GetModel());
        barrel.Teleport(position, rotation, null);
        barrel.DispatchSpawn();

        _barrels[barrel.Index] = new BarrelSource(owner.Index);
        effects.TrackEntity(barrel);
        effects.RegisterCleanup(() => _barrels.TryRemove(barrel.Index, out _));
        return true;
    }

    private HookResult OnEntityTakeDamage(CEntityInstance entity, CTakeDamageInfo damageInfo)
    {
        if (entity is null
            || !entity.IsValid
            || !_barrels.TryRemove(entity.Index, out var source))
        {
            return HookResult.Continue;
        }

        var barrel = entity.As<CDynamicProp>();
        var origin = barrel?.AbsOrigin;
        if (barrel is null || !barrel.IsValid || origin is null)
        {
            return HookResult.Continue;
        }

        var explosionPosition = new Vector(origin.X, origin.Y, origin.Z + 32.0f);
        barrel.Remove();

        _plugin.AddTimer(0.05f, () =>
        {
            var owner = Utilities.GetPlayerFromIndex((int)source.OwnerIndex);
            if (owner is not { IsValid: true })
            {
                return;
            }

            var damage = float.IsFinite(_settings.ExplosionDamage)
                ? Math.Max(0.0f, _settings.ExplosionDamage)
                : 50.0f;
            var radius = float.IsFinite(_settings.ExplosionRadius)
                ? Math.Max(0.0f, _settings.ExplosionRadius)
                : 600.0f;
            _explosions.TrySpawn(explosionPosition, owner, damage, radius);
        });

        return HookResult.Continue;
    }

    private void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(GetModel());
    }

    private string GetModel()
    {
        return string.IsNullOrWhiteSpace(_settings.PropModel)
            ? "models/props/de_train/hr_t/barrel_a/barrel_a.vmdl"
            : _settings.PropModel;
    }
}
