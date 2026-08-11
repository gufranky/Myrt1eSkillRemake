using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class FortniteService
{
    private const string WallNamePrefix = "myrt1eskill_fortnite_wall_";

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly FortniteSettings _settings;
    private readonly ConcurrentDictionary<uint, int> _health = new();
    private bool _loaded;

    public FortniteService(Myrt1eSkillRemakePlugin plugin, FortniteSettings settings)
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
        _health.Clear();
        _loaded = false;
    }

    public bool Place(CCSPlayerController owner, EffectScope effects)
    {
        var pawn = owner.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (!owner.IsValid || !owner.PawnIsAlive || pawn is not { IsValid: true } || origin is null)
        {
            return false;
        }

        var wall = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (wall is not { IsValid: true })
        {
            return false;
        }

        var distance = float.IsFinite(_settings.PlacementDistance)
            ? Math.Max(0.0f, _settings.PlacementDistance)
            : 50.0f;
        var yawRadians = pawn.EyeAngles.Y * MathF.PI / 180.0f;
        var position = new Vector(
            origin.X + MathF.Cos(yawRadians) * distance,
            origin.Y + MathF.Sin(yawRadians) * distance,
            origin.Z);
        var rotation = new QAngle(0.0f, pawn.EyeAngles.Y + 90.0f, 0.0f);

        wall.Entity!.Name = $"{WallNamePrefix}{owner.Index}_{wall.Index}";
        wall.Globalname = wall.Entity.Name;
        wall.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        var sceneOwner = wall.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (sceneOwner is not null)
        {
            sceneOwner.Flags &= ~(uint)(1 << 2);
        }

        wall.Health = wall.MaxHealth = 1_000_000;
        wall.SetModel(GetModel());
        wall.Teleport(position, rotation, null);
        wall.DispatchSpawn();

        _health[wall.Index] = Math.Max(1, _settings.BarricadeHealth);
        effects.TrackEntity(wall);
        effects.RegisterCleanup(() => _health.TryRemove(wall.Index, out _));

        _plugin.AddTimer(0.01f, () =>
        {
            if (wall.IsValid)
            {
                wall.Teleport(position, rotation, null);
                Utilities.SetStateChanged(wall, "CBaseEntity", "m_CBodyComponent");
            }
        });
        return true;
    }

    private HookResult OnEntityTakeDamage(CEntityInstance entity, CTakeDamageInfo damageInfo)
    {
        if (entity is null || !entity.IsValid || !_health.TryGetValue(entity.Index, out var health))
        {
            return HookResult.Continue;
        }

        var wall = entity.As<CDynamicProp>();
        if (wall is not { IsValid: true })
        {
            _health.TryRemove(entity.Index, out _);
            return HookResult.Continue;
        }

        wall.EmitSound("Wood_Plank.BulletImpact", volume: Math.Clamp(_settings.SoundVolume, 0.0f, 1.0f));
        var remaining = health - Math.Max(0, (int)damageInfo.Damage);
        if (remaining <= 0)
        {
            _health.TryRemove(entity.Index, out _);
            wall.Remove();
        }
        else
        {
            _health[entity.Index] = remaining;
        }

        return HookResult.Continue;
    }

    private void OnServerPrecacheResources(ResourceManifest manifest) => manifest.AddResource(GetModel());

    private string GetModel() => string.IsNullOrWhiteSpace(_settings.PropModel)
        ? "models/props/de_aztec/hr_aztec/aztec_scaffolding/aztec_scaffold_wall_support_128.vmdl"
        : _settings.PropModel;
}
