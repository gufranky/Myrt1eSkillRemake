using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class BombMinerService
{
    private sealed record MineInfo(uint OwnerIndex, byte Team);

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly BombMinerSettings _settings;
    private readonly ConcurrentDictionary<uint, byte> _holders = new();
    private readonly ConcurrentDictionary<uint, MineInfo> _mines = new();
    private bool _loaded;

    public BombMinerService(Myrt1eSkillRemakePlugin plugin, BombMinerSettings settings)
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

        _plugin.RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        _plugin.RegisterListener<Listeners.OnTick>(OnTick);
        _loaded = true;
    }

    public void Unload()
    {
        if (_loaded)
        {
            _plugin.RemoveListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
            _plugin.RemoveListener<Listeners.OnTick>(OnTick);
            _loaded = false;
        }

        ClearAllMines();
        _holders.Clear();
    }

    public void Acquire(CCSPlayerController player, EffectScope effects)
    {
        _holders[player.Index] = 0;
        effects.RegisterCleanup(() => Release(player.Index));
    }

    private void Release(uint ownerIndex)
    {
        _holders.TryRemove(ownerIndex, out _);
        foreach (var pair in _mines.Where(pair => pair.Value.OwnerIndex == ownerIndex).ToArray())
        {
            RemoveMine(pair.Key);
        }
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity.DesignerName != "hegrenade_projectile")
        {
            return;
        }

        var grenade = entity.As<CBaseCSGrenadeProjectile>();
        if (grenade is null || !grenade.IsValid)
        {
            return;
        }

        Server.NextFrame(() => ConfigureMine(grenade));
    }

    private void ConfigureMine(CBaseCSGrenadeProjectile grenade)
    {
        var pawn = grenade.OwnerEntity?.Value?.As<CCSPlayerPawn>();
        var owner = pawn?.Controller?.Value?.As<CCSPlayerController>();
        if (!grenade.IsValid
            || owner is not { IsValid: true }
            || !_holders.ContainsKey(owner.Index))
        {
            return;
        }

        var damageMultiplier = float.IsFinite(_settings.DamageMultiplier)
            ? Math.Max(0.0f, _settings.DamageMultiplier)
            : 2.0f;
        var radiusMultiplier = float.IsFinite(_settings.RadiusMultiplier)
            ? Math.Max(0.0f, _settings.RadiusMultiplier)
            : 2.0f;

        grenade.Damage *= damageMultiplier;
        grenade.DmgRadius *= radiusMultiplier;
        grenade.DetonateTime = float.MaxValue;
        Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");
        _mines[grenade.Index] = new MineInfo(owner.Index, owner.TeamNum);
    }

    private void OnTick()
    {
        if (_mines.IsEmpty || Server.TickCount % 10 != 0)
        {
            return;
        }

        var range = float.IsFinite(_settings.DetonationRange)
            ? Math.Max(1.0f, _settings.DetonationRange)
            : 220.0f;
        var armingSeconds = float.IsFinite(_settings.ArmingSeconds)
            ? Math.Max(0.0f, _settings.ArmingSeconds)
            : 3.0f;

        foreach (var pair in _mines.ToArray())
        {
            var grenade = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>((int)pair.Key);
            var origin = grenade?.AbsOrigin;
            if (grenade is null || !grenade.IsValid || origin is null)
            {
                _mines.TryRemove(pair.Key, out _);
                continue;
            }

            if (grenade.CreateTime + armingSeconds > Server.CurrentTime)
            {
                continue;
            }

            var enemyNearby = Utilities.GetPlayers().Any(player =>
            {
                if (!player.IsValid || !player.PawnIsAlive || player.TeamNum == pair.Value.Team)
                {
                    return false;
                }

                var enemyOrigin = player.PlayerPawn.Value?.AbsOrigin;
                return enemyOrigin is not null && Distance(origin, enemyOrigin) <= range;
            });

            if (!enemyNearby)
            {
                continue;
            }

            Detonate(grenade);
            _mines.TryRemove(pair.Key, out _);
        }
    }

    private void Detonate(CBaseCSGrenadeProjectile grenade)
    {
        var origin = grenade.AbsOrigin;
        if (!grenade.IsValid || origin is null)
        {
            return;
        }

        grenade.Teleport(new Vector(origin.X, origin.Y, origin.Z + 60.0f));
        grenade.EmitSound(
            "IncGrenade.Bounce_M",
            volume: float.IsFinite(_settings.SoundVolume)
                ? Math.Clamp(_settings.SoundVolume, 0.0f, 1.0f)
                : 1.0f);
        grenade.DetonateTime = Server.CurrentTime + (float.IsFinite(_settings.DetonationDelaySeconds)
            ? Math.Max(0.0f, _settings.DetonationDelaySeconds)
            : 0.50f);
        Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");
    }

    private void ClearAllMines()
    {
        foreach (var index in _mines.Keys.ToArray())
        {
            RemoveMine(index);
        }
    }

    private void RemoveMine(uint index)
    {
        _mines.TryRemove(index, out _);
        var grenade = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>((int)index);
        if (grenade is { IsValid: true })
        {
            grenade.Remove();
        }
    }

    private static float Distance(Vector first, Vector second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        var z = first.Z - second.Z;
        return MathF.Sqrt(x * x + y * y + z * z);
    }
}
