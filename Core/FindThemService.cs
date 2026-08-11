using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class FindThemService : IDisposable
{
    private sealed class ScoutChicken
    {
        public required uint ChickenIndex { get; init; }
        public required uint TargetControllerIndex { get; init; }
    }

    private readonly FindThemSettings _settings;
    private readonly Dictionary<uint, List<ScoutChicken>> _scouts = new();

    public FindThemService(FindThemSettings settings)
    {
        _settings = settings;
    }

    public int Deploy(CCSPlayerController owner)
    {
        var ownerPawn = owner.PlayerPawn.Value;
        var origin = ownerPawn?.AbsOrigin;
        if (!owner.IsValid
            || !owner.PawnIsAlive
            || ownerPawn is not { IsValid: true }
            || origin is null)
        {
            return 0;
        }

        Remove(owner.Index);
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
            return 0;
        }

        var scouts = new List<ScoutChicken>(enemies.Length);
        var spawnRadius = PositiveFiniteOr(_settings.SpawnRadius, 48.0f);
        var health = Math.Clamp(_settings.ChickenHealth, 1, 10000);
        for (var i = 0; i < enemies.Length; i++)
        {
            var target = enemies[i];
            var targetPawn = target.PlayerPawn.Value;
            if (targetPawn is not { IsValid: true })
            {
                continue;
            }

            var chicken = Utilities.CreateEntityByName<CChicken>("chicken");
            if (chicken is null)
            {
                continue;
            }

            var angle = 2.0f * MathF.PI * i / enemies.Length;
            var position = new Vector(
                origin.X + MathF.Cos(angle) * spawnRadius,
                origin.Y + MathF.Sin(angle) * spawnRadius,
                origin.Z + 2.0f);
            chicken.DispatchSpawn();
            chicken.Render = Color.FromArgb(255, 255, 190, 40);
            chicken.MaxHealth = health;
            chicken.Health = health;
            chicken.Teleport(position, ownerPawn.AbsRotation, new Vector(0.0f, 0.0f, 0.0f));
            chicken.Leader.Raw = target.PlayerPawn.Raw;
            Utilities.SetStateChanged(chicken, "CBaseModelEntity", "m_clrRender");
            Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iHealth");

            scouts.Add(new ScoutChicken
            {
                ChickenIndex = chicken.Index,
                TargetControllerIndex = target.Index
            });
        }

        if (scouts.Count > 0)
        {
            _scouts[owner.Index] = scouts;
        }

        return scouts.Count;
    }

    public void Update(CCSPlayerController owner)
    {
        if (!_scouts.TryGetValue(owner.Index, out var scouts))
        {
            return;
        }

        if (!owner.IsValid || !owner.PawnIsAlive)
        {
            Remove(owner.Index);
            return;
        }

        for (var i = scouts.Count - 1; i >= 0; i--)
        {
            var scout = scouts[i];
            var chicken = Utilities.GetEntityFromIndex<CChicken>((int)scout.ChickenIndex);
            var target = Utilities.GetPlayerFromIndex((int)scout.TargetControllerIndex);
            var targetPawn = target?.PlayerPawn.Value;
            if (chicken is not { IsValid: true }
                || chicken.Health <= 0
                || target is not { IsValid: true, PawnIsAlive: true }
                || targetPawn is not { IsValid: true })
            {
                if (chicken is { IsValid: true })
                {
                    chicken.Remove();
                }

                scouts.RemoveAt(i);
                continue;
            }

            if (chicken.Leader.Raw != target.PlayerPawn.Raw)
            {
                chicken.Leader.Raw = target.PlayerPawn.Raw;
            }
        }

        if (scouts.Count == 0)
        {
            _scouts.Remove(owner.Index);
        }
    }

    public void Remove(uint ownerIndex)
    {
        if (!_scouts.Remove(ownerIndex, out var scouts))
        {
            return;
        }

        foreach (var scout in scouts)
        {
            var chicken = Utilities.GetEntityFromIndex<CChicken>((int)scout.ChickenIndex);
            if (chicken is { IsValid: true })
            {
                chicken.Remove();
            }
        }

        scouts.Clear();
    }

    public void Dispose()
    {
        foreach (var ownerIndex in _scouts.Keys.ToArray())
        {
            Remove(ownerIndex);
        }
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;
}
