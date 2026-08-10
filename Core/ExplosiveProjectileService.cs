using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class ExplosiveProjectileService
{
    public readonly record struct DamageSource(uint OwnerIndex, int Team);

    private sealed record SpawnRequest(uint OwnerIndex, byte Team);
    private sealed record KillCredit(uint AttackerIndex, int ExpiryTick);

    private const string GlobalNamePrefix = "myrt1eskill_explosiveshot_";
    private static readonly QAngle MarkerAngle = new(5, 10, -4);
    private static readonly Vector ZeroVelocity = new(0, 0, 0);

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly ExplosiveShotSettings _settings;
    private readonly ConcurrentQueue<SpawnRequest> _pendingSpawns = new();
    private readonly ConcurrentDictionary<uint, KillCredit> _killCredits = new();
    private MemoryFunctionWithReturn<nint, nint, nint, nint, nint, nint, nint, int>? _createHe;
    private bool _loaded;
    private bool _signatureFailureLogged;

    public ExplosiveProjectileService(Myrt1eSkillRemakePlugin plugin, ExplosiveShotSettings settings)
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
        _loaded = true;
    }

    public void Unload()
    {
        if (_loaded)
        {
            _plugin.RemoveListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
            _loaded = false;
        }

        ClearRuntimeState();
    }

    public bool TrySpawn(Vector position, CCSPlayerController owner)
    {
        if (!TryResolveFactory())
        {
            return false;
        }

        var request = new SpawnRequest(owner.Index, owner.TeamNum);
        _pendingSpawns.Enqueue(request);

        try
        {
            // Parameters and weapon id intentionally mirror jRandomSkills' CS2-native HE factory call.
            _createHe!.Invoke(
                position.Handle,
                MarkerAngle.Handle,
                ZeroVelocity.Handle,
                ZeroVelocity.Handle,
                nint.Zero,
                44,
                owner.TeamNum);
            return true;
        }
        catch (Exception exception)
        {
            _pendingSpawns.TryDequeue(out _);
            _plugin.Logger.LogError(exception, "Failed to create an ExplosiveShot HE projectile");
            return false;
        }
    }

    public DamageSource? ApplyTeamDamageModifier(CCSPlayerController victim, CTakeDamageInfo damageInfo)
    {
        if (!TryReadSource(damageInfo, out var source))
        {
            return null;
        }

        if (victim.Index != source.OwnerIndex && victim.TeamNum == source.Team)
        {
            var reduction = float.IsFinite(_settings.TeammateDamageReduction)
                ? Math.Clamp(_settings.TeammateDamageReduction, 0.0f, 1.0f)
                : 0.50f;
            damageInfo.Damage *= 1.0f - reduction;
        }

        return source;
    }

    public void RegisterLethalDamageCredit(
        CCSPlayerController victim,
        CCSPlayerPawn victimPawn,
        CTakeDamageInfo damageInfo,
        DamageSource? source)
    {
        if (source is not { } explosion
            || victim.Index == explosion.OwnerIndex
            || damageInfo.Damage < victimPawn.Health)
        {
            return;
        }

        _killCredits[victim.Index] = new KillCredit(explosion.OwnerIndex, Server.TickCount + 64);
    }

    public HookResult OnPlayerDeathPre(EventPlayerDeath @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        if (victim is null || !victim.IsValid || !_killCredits.TryRemove(victim.Index, out var credit))
        {
            return HookResult.Continue;
        }

        if (credit.ExpiryTick < Server.TickCount)
        {
            return HookResult.Continue;
        }

        var attacker = Utilities.GetPlayerFromIndex((int)credit.AttackerIndex);
        if (attacker is null || !attacker.IsValid || attacker.Index == victim.Index)
        {
            return HookResult.Continue;
        }

        @event.Attacker = attacker;
        @event.Weapon = "explosion";

        var matchStats = attacker.ActionTrackingServices?.MatchStats;
        if (matchStats is not null)
        {
            matchStats.Kills++;
            Utilities.SetStateChanged(attacker, "CCSPlayerController", "m_pActionTrackingServices");
        }

        return HookResult.Continue;
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        ClearRuntimeState();
        return HookResult.Continue;
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity.DesignerName != "hegrenade_projectile")
        {
            return;
        }

        var projectile = entity.As<CBaseCSGrenadeProjectile>();
        if (projectile is null || !projectile.IsValid || projectile.AbsRotation is null)
        {
            return;
        }

        Server.NextFrame(() => ConfigureSpawnedProjectile(projectile));
    }

    private void ConfigureSpawnedProjectile(CBaseCSGrenadeProjectile projectile)
    {
        if (!projectile.IsValid
            || projectile.AbsRotation is null
            || !NearlyEquals(projectile.AbsRotation.X, MarkerAngle.X)
            || !NearlyEquals(projectile.AbsRotation.Y, MarkerAngle.Y)
            || !NearlyEquals(projectile.AbsRotation.Z, MarkerAngle.Z)
            || !_pendingSpawns.TryDequeue(out var source))
        {
            return;
        }

        projectile.TicksAtZeroVelocity = 100;
        projectile.TeamNum = source.Team;
        projectile.Damage = float.IsFinite(_settings.Damage) ? Math.Max(0.0f, _settings.Damage) : 25.0f;
        projectile.DmgRadius = float.IsFinite(_settings.DamageRadius) ? Math.Max(0.0f, _settings.DamageRadius) : 210.0f;
        projectile.DetonateTime = 0.0f;
        projectile.Globalname = $"{GlobalNamePrefix}{source.Team}_{source.OwnerIndex}_{projectile.Index}";
    }

    private bool TryResolveFactory()
    {
        if (_createHe is not null)
        {
            return true;
        }

        try
        {
            _createHe = new MemoryFunctionWithReturn<nint, nint, nint, nint, nint, nint, nint, int>(
                GameData.GetSignature("HEGrenadeProjectile_CreateFunc"));
            return true;
        }
        catch (Exception exception)
        {
            if (!_signatureFailureLogged)
            {
                _signatureFailureLogged = true;
                _plugin.Logger.LogError(exception,
                    "ExplosiveShot disabled: HEGrenadeProjectile_CreateFunc could not be resolved");
            }

            return false;
        }
    }

    private static bool TryReadSource(CTakeDamageInfo damageInfo, out DamageSource source)
    {
        source = default;
        var attacker = damageInfo.Attacker?.Value;
        if (attacker is null
            || !attacker.IsValid
            || attacker.DesignerName != "hegrenade_projectile"
            || string.IsNullOrEmpty(attacker.Globalname)
            || !attacker.Globalname.StartsWith(GlobalNamePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var payload = attacker.Globalname[GlobalNamePrefix.Length..].Split('_');
        if (payload.Length < 2
            || !int.TryParse(payload[0], out var team)
            || !uint.TryParse(payload[1], out var ownerIndex))
        {
            return false;
        }

        source = new DamageSource(ownerIndex, team);
        return true;
    }

    private void ClearRuntimeState()
    {
        while (_pendingSpawns.TryDequeue(out _))
        {
        }

        _killCredits.Clear();
    }

    private static bool NearlyEquals(float first, float second, float epsilon = 0.001f) =>
        Math.Abs(first - second) < epsilon;
}
