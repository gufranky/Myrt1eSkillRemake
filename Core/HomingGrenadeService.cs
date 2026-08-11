using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class HomingGrenadeService
{
    public const int TickStride = 10;

    private sealed record HomingGrenadeInfo(
        uint OwnerIndex,
        byte Team,
        Vector LastPosition);

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly HomingNadesSettings _settings;
    private readonly ConcurrentDictionary<uint, byte> _holders = new();
    private readonly ConcurrentDictionary<uint, HomingGrenadeInfo> _grenades = new();
    private bool _loaded;

    public HomingGrenadeService(
        Myrt1eSkillRemakePlugin plugin,
        HomingNadesSettings settings)
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

        foreach (var index in _grenades.Keys.ToArray())
        {
            StopTracking(index, restoreDetonation: true);
        }

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
        foreach (var pair in _grenades
                     .Where(pair => pair.Value.OwnerIndex == ownerIndex)
                     .ToArray())
        {
            StopTracking(pair.Key, restoreDetonation: true);
        }
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (!entity.DesignerName.EndsWith("_projectile", StringComparison.Ordinal)
            || entity.DesignerName.Equals("smokegrenade_projectile", StringComparison.Ordinal))
        {
            return;
        }

        var grenade = entity.As<CBaseCSGrenadeProjectile>();
        if (grenade is { IsValid: true })
        {
            Server.NextFrame(() => Configure(grenade));
        }
    }

    private void Configure(CBaseCSGrenadeProjectile grenade)
    {
        if (!grenade.IsValid || grenade.AbsOrigin is not { } origin)
        {
            return;
        }

        var pawn = grenade.Thrower.Value
                   ?? grenade.OwnerEntity.Value?.As<CCSPlayerPawn>();
        var owner = pawn?.Controller.Value?.As<CCSPlayerController>();
        if (owner is not { IsValid: true } || !_holders.ContainsKey(owner.Index))
        {
            return;
        }

        _grenades[grenade.Index] = new HomingGrenadeInfo(
            owner.Index,
            owner.TeamNum,
            Copy(origin));
        grenade.DetonateTime += 30.0f;
        Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");
    }

    private void OnTick()
    {
        if (_grenades.IsEmpty || Server.TickCount % TickStride != 0)
        {
            return;
        }

        foreach (var pair in _grenades.ToArray())
        {
            UpdateGrenade(pair.Key, pair.Value);
        }
    }

    private void UpdateGrenade(uint index, HomingGrenadeInfo info)
    {
        var grenade = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>((int)index);
        if (grenade is not { IsValid: true } || grenade.AbsOrigin is not { } origin)
        {
            _grenades.TryRemove(index, out _);
            return;
        }

        var currentPosition = Copy(origin);
        var attraction = CalculateAttraction(grenade, info.Team);
        var isAtTarget = attraction?.IsZero() == true;
        if (Distance(currentPosition, info.LastPosition) < 4.0f
            || attraction is null
            || isAtTarget)
        {
            grenade.DetonateTime = isAtTarget ? 0.0f : grenade.CreateTime + 1.5f;
            Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");
            _grenades.TryRemove(index, out _);
            return;
        }

        var velocity = new Vector(
            grenade.Velocity.X + attraction.X,
            grenade.Velocity.Y + attraction.Y,
            grenade.Velocity.Z + attraction.Z);
        var maximumVelocity = FiniteOr(_settings.MaximumVelocity, 2000.0f, 1.0f);
        var speed = velocity.Length();
        if (speed > maximumVelocity)
        {
            velocity *= maximumVelocity / speed;
        }

        _grenades[index] = info with { LastPosition = currentPosition };
        grenade.Teleport(null, null, velocity);
    }

    private Vector? CalculateAttraction(CBaseCSGrenadeProjectile grenade, byte team)
    {
        if (grenade.AbsOrigin is not { } grenadeOrigin)
        {
            return null;
        }

        Vector? nearestEnemy = null;
        var nearestDistance = float.MaxValue;
        var detonationRange = FiniteOr(_settings.DetonationRange, 130.0f, 1.0f);
        foreach (var enemy in Utilities.GetPlayers())
        {
            if (!enemy.IsValid
                || !enemy.PawnIsAlive
                || enemy.TeamNum == team
                || enemy.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist)
                || enemy.PlayerPawn.Value?.AbsOrigin is not { } enemyOrigin)
            {
                continue;
            }

            var distance = Distance(grenadeOrigin, enemyOrigin);
            if (distance < detonationRange)
            {
                return Vector.Zero;
            }

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemyOrigin;
            }
        }

        if (nearestEnemy is null)
        {
            return null;
        }

        var direction = nearestEnemy - grenadeOrigin;
        var length = direction.Length();
        if (length <= 0.0f)
        {
            return Vector.Zero;
        }

        var strength = FiniteOr(_settings.Strength, 150.0f, 0.0f);
        return direction * (strength / length);
    }

    private void StopTracking(uint index, bool restoreDetonation)
    {
        if (!_grenades.TryRemove(index, out _) || !restoreDetonation)
        {
            return;
        }

        var grenade = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>((int)index);
        if (grenade is not { IsValid: true })
        {
            return;
        }

        grenade.DetonateTime = Math.Min(grenade.DetonateTime, grenade.CreateTime + 1.5f);
        Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");
    }

    private static Vector Copy(Vector value) => new(value.X, value.Y, value.Z);

    private static float Distance(Vector first, Vector second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        var z = first.Z - second.Z;
        return MathF.Sqrt(x * x + y * y + z * z);
    }

    private static float FiniteOr(float value, float fallback, float minimum) =>
        Math.Max(minimum, float.IsFinite(value) ? value : fallback);
}
