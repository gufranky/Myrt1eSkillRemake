using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class IllusionistService
{
    private sealed class ReplicaState
    {
        public required uint OwnerIndex { get; init; }
        public required DateTime ExpiresAt { get; init; }
        public required Vector Direction { get; init; }
        public required float Speed { get; init; }
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    }

    private const string ReplicaNamePrefix = "myrt1eskill_illusionist_";

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly IllusionistSettings _settings;
    private readonly ConcurrentDictionary<uint, ReplicaState> _replicas = new();
    private bool _loaded;

    public IllusionistService(Myrt1eSkillRemakePlugin plugin, IllusionistSettings settings)
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

        _plugin.RegisterListener<Listeners.OnTick>(OnTick);
        _plugin.RegisterListener<Listeners.OnEntityTakeDamagePre>(OnEntityTakeDamage);
        _loaded = true;
    }

    public void Unload()
    {
        if (!_loaded)
        {
            return;
        }

        _plugin.RemoveListener<Listeners.OnTick>(OnTick);
        _plugin.RemoveListener<Listeners.OnEntityTakeDamagePre>(OnEntityTakeDamage);
        foreach (var replicaIndex in _replicas.Keys)
        {
            RemoveReplica(replicaIndex);
        }

        _replicas.Clear();
        _loaded = false;
    }

    public bool Deploy(CCSPlayerController owner, EffectScope effects)
    {
        var pawn = owner.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        var modelName = pawn?.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName;
        if (!owner.IsValid
            || !owner.PawnIsAlive
            || pawn is not { IsValid: true }
            || origin is null
            || string.IsNullOrWhiteSpace(modelName))
        {
            return false;
        }

        var replica = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (replica is not { IsValid: true })
        {
            return false;
        }

        var crouching = ((PlayerFlags)pawn.Flags).HasFlag(PlayerFlags.FL_DUCKING);
        var yawRadians = pawn.EyeAngles.Y * MathF.PI / 180.0f;
        var direction = new Vector(MathF.Cos(yawRadians), MathF.Sin(yawRadians), 0.0f);
        var spawnDistance = NonNegativeFiniteOr(_settings.SpawnDistance, 40.0f);
        var spawnPosition = new Vector(
            origin.X + direction.X * spawnDistance,
            origin.Y + direction.Y * spawnDistance,
            origin.Z);
        var rotation = new QAngle(0.0f, pawn.EyeAngles.Y, 0.0f);

        replica.Entity!.Name = $"{ReplicaNamePrefix}{owner.Index}_{replica.Index}";
        replica.Globalname = replica.Entity.Name;
        replica.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        var sceneOwner = replica.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (sceneOwner is not null)
        {
            sceneOwner.Flags &= ~(uint)(1 << 2);
        }

        replica.Health = replica.MaxHealth = 1_000_000;
        replica.UseAnimGraph = false;
        replica.SetModel(modelName);
        replica.Teleport(spawnPosition, rotation, null);
        replica.DispatchSpawn();
        replica.AcceptInput("SetAnimation", value: crouching ? "crouch_new_knife_n" : "run_new_knife_n");
        replica.AcceptInput("SetPlaybackRate", value: "1.0");

        var duration = PositiveFiniteOr(
            crouching ? _settings.CrouchDurationSeconds : _settings.RunDurationSeconds,
            crouching ? 12.0f : 5.0f);
        var speed = NonNegativeFiniteOr(
            crouching ? _settings.CrouchSpeed : _settings.RunSpeed,
            crouching ? 80.0f : 224.0f);
        _replicas[replica.Index] = new ReplicaState
        {
            OwnerIndex = owner.Index,
            ExpiresAt = DateTime.UtcNow.AddSeconds(duration),
            Direction = direction,
            Speed = speed
        };

        effects.TrackEntity(replica);
        effects.RegisterCleanup(() => _replicas.TryRemove(replica.Index, out _));
        PluginText.Center(owner, "🎭 复制品已部署");
        return true;
    }

    private void OnTick()
    {
        var now = DateTime.UtcNow;
        foreach (var pair in _replicas.ToArray())
        {
            var replica = Utilities.GetEntityFromIndex<CDynamicProp>((int)pair.Key);
            var origin = replica?.AbsOrigin;
            if (replica is not { IsValid: true } || origin is null || now >= pair.Value.ExpiresAt)
            {
                RemoveReplica(pair.Key);
                continue;
            }

            var elapsed = Math.Clamp((float)(now - pair.Value.LastUpdate).TotalSeconds, 0.0f, 0.1f);
            pair.Value.LastUpdate = now;
            replica.Teleport(
                new Vector(
                    origin.X + pair.Value.Direction.X * pair.Value.Speed * elapsed,
                    origin.Y + pair.Value.Direction.Y * pair.Value.Speed * elapsed,
                    origin.Z),
                null,
                null);
        }
    }

    private HookResult OnEntityTakeDamage(CEntityInstance entity, CTakeDamageInfo damageInfo)
    {
        if (entity is null
            || !entity.IsValid
            || damageInfo is null
            || !_replicas.TryGetValue(entity.Index, out var state))
        {
            return HookResult.Continue;
        }

        var attackerPawn = damageInfo.Attacker?.Value?.As<CCSPlayerPawn>();
        var attacker = attackerPawn?.Controller?.Value?.As<CCSPlayerController>();
        var owner = Utilities.GetPlayerFromIndex((int)state.OwnerIndex);
        if (attacker is not { IsValid: true, PawnIsAlive: true }
            || owner is not { IsValid: true }
            || attacker.Team == owner.Team)
        {
            return HookResult.Continue;
        }

        if (!_replicas.TryRemove(entity.Index, out _))
        {
            return HookResult.Continue;
        }

        var replica = entity.As<CDynamicProp>();
        if (replica is { IsValid: true })
        {
            replica.EmitSound(
                "GlassBottle.BulletImpact",
                volume: Math.Clamp(FiniteOr(_settings.SoundVolume, 1.0f), 0.0f, 1.0f));
            replica.Remove();
        }

        var damage = NonNegativeFiniteOr(_settings.EnemyDamage, 20.0f);
        if (damage > 0.0f)
        {
            SkillDamage.TryDeal(owner, attacker, damage, DamageTypes_t.DMG_GENERIC);
        }

        PluginText.Chat(attacker, $"[魔术师] 你射中了复制品，受到 {damage:0.#} 点伤害！");
        return HookResult.Continue;
    }

    private void RemoveReplica(uint replicaIndex)
    {
        _replicas.TryRemove(replicaIndex, out _);
        var replica = Utilities.GetEntityFromIndex<CDynamicProp>((int)replicaIndex);
        if (replica is { IsValid: true })
        {
            replica.Remove();
        }
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static float NonNegativeFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value >= 0.0f ? value : fallback;

    private static float FiniteOr(float value, float fallback) =>
        float.IsFinite(value) ? value : fallback;
}
