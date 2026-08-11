using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class HealingChickenService : IDisposable
{
    private const string HitParticle = "particles/critters/chicken/chicken_goop.vpcf";

    private sealed class ChickenState
    {
        public required uint EntityIndex { get; init; }
        public float NextHealAt { get; set; }
        public ChickenMovementBoost.State Movement { get; } = new();
    }

    private sealed class OwnerState
    {
        public required uint ControllerIndex { get; init; }
        public List<ChickenState> Chickens { get; } = new();
    }

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly HealingChickenSettings _settings;
    private readonly Dictionary<uint, OwnerState> _owners = new();
    private readonly Dictionary<uint, uint> _chickenOwners = new();
    private bool _loaded;

    public HealingChickenService(
        Myrt1eSkillRemakePlugin plugin,
        HealingChickenSettings settings)
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
        _plugin.RegisterListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
        _loaded = true;
    }

    public bool Spawn(CCSPlayerController owner)
    {
        var pawn = owner.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (!owner.IsValid
            || !owner.PawnIsAlive
            || pawn is not { IsValid: true }
            || origin is null)
        {
            return false;
        }

        Remove(owner.Index);
        var state = new OwnerState { ControllerIndex = owner.Index };
        var amount = Math.Clamp(_settings.Amount, 1, 8);
        var spawnRadius = PositiveFiniteOr(_settings.SpawnRadius, 100.0f);
        var chickenHealth = Math.Clamp(_settings.ChickenHealth, 1, 10000);

        for (var i = 0; i < amount; i++)
        {
            var chicken = Utilities.CreateEntityByName<CChicken>("chicken");
            if (chicken is null)
            {
                continue;
            }

            var angle = 2.0f * MathF.PI * i / amount;
            var position = new Vector(
                origin.X + MathF.Cos(angle) * spawnRadius,
                origin.Y + MathF.Sin(angle) * spawnRadius,
                origin.Z + 2.0f);
            chicken.DispatchSpawn();
            chicken.Render = Color.LightGreen;
            chicken.MaxHealth = chickenHealth;
            chicken.Health = chickenHealth;
            chicken.Teleport(position, pawn.AbsRotation, new Vector(0.0f, 0.0f, 0.0f));
            chicken.Leader.Raw = owner.PlayerPawn.Raw;
            Utilities.SetStateChanged(chicken, "CBaseModelEntity", "m_clrRender");
            Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iHealth");

            var chickenState = new ChickenState
            {
                EntityIndex = chicken.Index,
                NextHealAt = Server.CurrentTime + HealInterval
            };
            state.Chickens.Add(chickenState);
            _chickenOwners[chicken.Index] = owner.Index;
        }

        if (state.Chickens.Count == 0)
        {
            return false;
        }

        _owners[owner.Index] = state;
        return true;
    }

    public void Update(CCSPlayerController owner)
    {
        if (!_owners.TryGetValue(owner.Index, out var state))
        {
            return;
        }

        var pawn = owner.PlayerPawn.Value;
        var ownerOrigin = pawn?.AbsOrigin;
        if (!owner.IsValid
            || !owner.PawnIsAlive
            || pawn is not { IsValid: true }
            || ownerOrigin is null)
        {
            Remove(owner.Index);
            return;
        }

        var now = Server.CurrentTime;
        var radius = PositiveFiniteOr(_settings.HealRadius, 150.0f);
        var radiusSquared = radius * radius;
        var heal = Math.Max(0, _settings.HealPerTick);
        var maximumHealth = _settings.MaximumHealth > 0
            ? Math.Max(1, _settings.MaximumHealth)
            : Math.Max(1, pawn.MaxHealth);

        for (var i = state.Chickens.Count - 1; i >= 0; i--)
        {
            var chickenState = state.Chickens[i];
            var chicken = Utilities.GetEntityFromIndex<CChicken>((int)chickenState.EntityIndex);
            var chickenOrigin = chicken?.AbsOrigin;
            if (chicken is not { IsValid: true }
                || chicken.Health <= 0
                || chickenOrigin is null)
            {
                _chickenOwners.Remove(chickenState.EntityIndex);
                state.Chickens.RemoveAt(i);
                continue;
            }

            if (chicken.Leader.Raw != owner.PlayerPawn.Raw)
            {
                chicken.Leader.Raw = owner.PlayerPawn.Raw;
            }

            ChickenMovementBoost.Update(
                chicken,
                ownerOrigin,
                chickenState.Movement,
                _settings.SpeedMultiplier,
                _settings.MaximumExtraStep);

            if (now < chickenState.NextHealAt)
            {
                continue;
            }

            chickenState.NextHealAt = now + HealInterval;
            if (heal <= 0
                || pawn.Health <= 0
                || pawn.Health >= maximumHealth
                || DistanceSquared(chickenOrigin, ownerOrigin) > radiusSquared)
            {
                continue;
            }

            pawn.Health = Math.Min(maximumHealth, pawn.Health + heal);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
    }

    public void Remove(uint controllerIndex)
    {
        if (!_owners.Remove(controllerIndex, out var state))
        {
            return;
        }

        foreach (var chickenState in state.Chickens)
        {
            _chickenOwners.Remove(chickenState.EntityIndex);
            var chicken = Utilities.GetEntityFromIndex<CChicken>((int)chickenState.EntityIndex);
            if (chicken is { IsValid: true })
            {
                chicken.Remove();
            }
        }

        state.Chickens.Clear();
    }

    public void Dispose()
    {
        foreach (var ownerIndex in _owners.Keys.ToArray())
        {
            Remove(ownerIndex);
        }

        _chickenOwners.Clear();
        if (_loaded)
        {
            try
            {
                _plugin.RemoveListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
                _plugin.RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            }
            catch (Exception exception)
            {
                _plugin.Logger.LogError(exception, "Failed to remove HealingChicken listeners");
            }

            _loaded = false;
        }
    }

    private HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo damageInfo)
    {
        if (entity is null
            || !entity.IsValid
            || damageInfo is null
            || !_chickenOwners.TryGetValue(entity.Index, out var ownerIndex))
        {
            return HookResult.Continue;
        }

        var owner = Utilities.GetPlayerFromIndex((int)ownerIndex);
        var ownerPawn = owner?.PlayerPawn.Value;
        var attackerPawn = damageInfo.Attacker?.Value?.As<CCSPlayerPawn>();
        if (owner is { IsValid: true }
            && ownerPawn is { IsValid: true }
            && attackerPawn is { IsValid: true }
            && string.Equals(attackerPawn.DesignerName, "player", StringComparison.Ordinal)
            && attackerPawn.Index != ownerPawn.Index
            && attackerPawn.TeamNum == owner.TeamNum)
        {
            damageInfo.Damage = 0.0f;
            damageInfo.TotalledDamage = 0.0f;
            damageInfo.ShouldBleed = false;
            return HookResult.Continue;
        }

        if (damageInfo.Damage > 0.0f)
        {
            CreateHitParticle(entity.Index);
        }

        Server.NextFrame(() => ForgetDeadChicken(entity.Index));
        return HookResult.Continue;
    }

    private void ForgetDeadChicken(uint chickenIndex)
    {
        if (!_chickenOwners.TryGetValue(chickenIndex, out var ownerIndex))
        {
            return;
        }

        var chicken = Utilities.GetEntityFromIndex<CChicken>((int)chickenIndex);
        if (chicken is { IsValid: true } && chicken.Health > 0)
        {
            return;
        }

        _chickenOwners.Remove(chickenIndex);
        if (_owners.TryGetValue(ownerIndex, out var owner))
        {
            owner.Chickens.RemoveAll(state => state.EntityIndex == chickenIndex);
        }
    }

    private void CreateHitParticle(uint chickenIndex)
    {
        var chicken = Utilities.GetEntityFromIndex<CChicken>((int)chickenIndex);
        var origin = chicken?.AbsOrigin;
        if (chicken is not { IsValid: true } || origin is null)
        {
            return;
        }

        var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (particle is null)
        {
            return;
        }

        particle.EffectName = HitParticle;
        particle.StartActive = true;
        particle.Teleport(new Vector(origin.X, origin.Y, origin.Z + 10.0f));
        particle.DispatchSpawn();
        particle.AcceptInput("Start");
        _plugin.AddTimer(3.0f, () =>
        {
            if (particle.IsValid)
            {
                particle.AcceptInput("Stop");
                particle.Remove();
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private float HealInterval => PositiveFiniteOr(_settings.HealIntervalSeconds, 0.25f);

    private static float DistanceSquared(Vector first, Vector second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        var dz = first.Z - second.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static void OnServerPrecacheResources(ResourceManifest manifest) =>
        manifest.AddResource(HitParticle);
}
