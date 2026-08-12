using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class KamikazeChickenService : IDisposable
{
    private sealed class ActiveChicken
    {
        public required uint ChickenIndex { get; init; }
        public required uint TargetControllerIndex { get; init; }
        public ChickenMovementBoost.State Movement { get; } = new();
    }

    private readonly KamikazeChickenSettings _settings;
    private readonly ExplosiveProjectileService _explosions;
    private readonly Dictionary<uint, ActiveChicken> _active = new();

    public KamikazeChickenService(
        KamikazeChickenSettings settings,
        ExplosiveProjectileService explosions)
    {
        _settings = settings;
        _explosions = explosions;
    }

    public bool Deploy(CCSPlayerController owner)
    {
        var ownerPawn = owner.PlayerPawn.Value;
        var origin = ownerPawn?.AbsOrigin;
        if (!owner.IsValid
            || !owner.PawnIsAlive
            || ownerPawn is not { IsValid: true }
            || origin is null)
        {
            return false;
        }

        var enemies = Utilities.GetPlayers()
            .Where(enemy => enemy.IsValid
                && enemy.PawnIsAlive
                && enemy.Team != CsTeam.None
                && enemy.Team != CsTeam.Spectator
                && enemy.Team != owner.Team
                && enemy.PlayerPawn.Value is { IsValid: true })
            .ToArray();
        if (enemies.Length == 0)
        {
            return false;
        }

        Remove(owner.Index);
        var target = enemies[Random.Shared.Next(enemies.Length)];
        var yaw = ownerPawn.EyeAngles.Y * MathF.PI / 180.0f;
        var spawnDistance = PositiveFiniteOr(_settings.SpawnDistance, 48.0f);
        var position = new Vector(
            origin.X + MathF.Cos(yaw) * spawnDistance,
            origin.Y + MathF.Sin(yaw) * spawnDistance,
            origin.Z + 2.0f);
        var chicken = Utilities.CreateEntityByName<CChicken>("chicken");
        if (chicken is null)
        {
            return false;
        }

        chicken.DispatchSpawn();
        chicken.Render = Color.FromArgb(255, 255, 35, 35);
        chicken.MaxHealth = 1;
        chicken.Health = 1;
        chicken.Teleport(position, ownerPawn.AbsRotation, new Vector(0.0f, 0.0f, 0.0f));
        chicken.Leader.Raw = target.PlayerPawn.Raw;
        SetScale(chicken, PositiveFiniteOr(_settings.ModelScale, 1.35f));
        Utilities.SetStateChanged(chicken, "CBaseModelEntity", "m_clrRender");
        Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iMaxHealth");
        Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iHealth");

        _active[owner.Index] = new ActiveChicken
        {
            ChickenIndex = chicken.Index,
            TargetControllerIndex = target.Index
        };
        return true;
    }

    public void Update(CCSPlayerController owner)
    {
        if (!_active.TryGetValue(owner.Index, out var active))
        {
            return;
        }

        var chicken = Utilities.GetEntityFromIndex<CChicken>((int)active.ChickenIndex);
        var target = Utilities.GetPlayerFromIndex((int)active.TargetControllerIndex);
        var targetPawn = target?.PlayerPawn.Value;
        var chickenOrigin = chicken?.AbsOrigin;
        var targetOrigin = targetPawn?.AbsOrigin;
        if (!owner.IsValid
            || !owner.PawnIsAlive
            || chicken is not { IsValid: true }
            || chicken.Health <= 0
            || chickenOrigin is null
            || target is not { IsValid: true, PawnIsAlive: true }
            || targetPawn is not { IsValid: true }
            || targetOrigin is null)
        {
            Remove(owner.Index);
            return;
        }

        if (chicken.Leader.Raw != target.PlayerPawn.Raw)
        {
            chicken.Leader.Raw = target.PlayerPawn.Raw;
        }

        ChickenMovementBoost.Update(
            chicken,
            targetOrigin,
            active.Movement,
            _settings.SpeedMultiplier,
            _settings.MaximumExtraStep,
            _settings.MaximumSpeed,
            allowTeleport: false);
        var triggerDistance = PositiveFiniteOr(_settings.DetonationDistance, 120.0f);
        if (DistanceSquared(chickenOrigin, targetOrigin) > triggerDistance * triggerDistance)
        {
            return;
        }

        var explosionPosition = new Vector(chickenOrigin.X, chickenOrigin.Y, chickenOrigin.Z + 10.0f);
        var damage = NonNegativeFiniteOr(_settings.ExplosionDamage, 100.0f);
        var radius = PositiveFiniteOr(_settings.ExplosionRadius, 350.0f);
        var teammateMultiplier = float.IsFinite(_settings.TeammateDamageMultiplier)
            ? Math.Clamp(_settings.TeammateDamageMultiplier, 0.0f, 1.0f)
            : 0.50f;
        _explosions.TrySpawn(
            explosionPosition,
            owner.Index,
            owner.TeamNum,
            damage,
            radius,
            teammateMultiplier);
        Remove(owner.Index);
    }

    public void Remove(uint ownerIndex)
    {
        if (!_active.Remove(ownerIndex, out var active))
        {
            return;
        }

        var chicken = Utilities.GetEntityFromIndex<CChicken>((int)active.ChickenIndex);
        if (chicken is { IsValid: true })
        {
            chicken.Remove();
        }
    }

    public void Dispose()
    {
        foreach (var ownerIndex in _active.Keys.ToArray())
        {
            Remove(ownerIndex);
        }
    }

    private static void SetScale(CChicken chicken, float scale)
    {
        var skeleton = chicken.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton is null)
        {
            return;
        }

        skeleton.Scale = scale;
        chicken.AcceptInput("SetScale", chicken, chicken, scale.ToString(CultureInfo.InvariantCulture));
        Utilities.SetStateChanged(chicken, "CBaseEntity", "m_CBodyComponent");
    }

    private static float DistanceSquared(Vector first, Vector second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        var z = first.Z - second.Z;
        return x * x + y * y + z * z;
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static float NonNegativeFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value >= 0.0f ? value : fallback;
}
