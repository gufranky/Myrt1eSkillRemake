using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace Myrt1eSkill_Remake.Core;

public sealed class FireRainService
{
    private sealed record SpawnRequest(uint OwnerIndex, uint OwnerHandle, byte Team, int Tick);

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly ConcurrentQueue<SpawnRequest> _pendingSpawns = new();
    private readonly ConcurrentDictionary<uint, (uint OwnerHandle, byte Team)> _rainMolotovs = new();
    private MemoryFunctionWithReturn<nint, nint, nint, nint, nint, nint, nint, int>? _createMolotov;
    private bool _loaded;
    private bool _signatureFailureLogged;

    public FireRainService(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
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

        Clear();
    }

    public void Clear()
    {
        while (_pendingSpawns.TryDequeue(out _))
        {
        }

        _rainMolotovs.Clear();
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Clear();
        return HookResult.Continue;
    }

    public bool SpawnRain(CCSPlayerController owner, Vector target)
    {
        if (!owner.IsValid || !owner.PawnIsAlive || !TryResolveFactory())
        {
            return false;
        }

        var pawn = owner.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid)
        {
            return false;
        }

        const int grenadeCount = 5;
        const float spawnHeight = 1500.0f;
        const float clusterRadius = 130.0f;
        const float flightTime = 1.2f;
        const float gravity = 800.0f;

        var spawnedAny = false;
        for (var index = 0; index < grenadeCount; index++)
        {
            var offsetAngle = Random.Shared.NextSingle() * MathF.Tau;
            var offsetDistance = MathF.Sqrt(Random.Shared.NextSingle()) * clusterRadius;
            var spawnPosition = new Vector(
                target.X + MathF.Cos(offsetAngle) * offsetDistance,
                target.Y + MathF.Sin(offsetAngle) * offsetDistance,
                target.Z + spawnHeight);

            var velocityX = (target.X - spawnPosition.X) / flightTime;
            var velocityY = (target.Y - spawnPosition.Y) / flightTime;
            var velocityZ = (target.Z - spawnPosition.Z
                + 0.5f * gravity * flightTime * flightTime) / flightTime;
            var velocity = new Vector(velocityX, velocityY, velocityZ);
            var horizontalSpeed = MathF.Sqrt(velocityX * velocityX + velocityY * velocityY);
            var angle = new QAngle(
                -MathF.Atan2(velocityZ, horizontalSpeed) * (180.0f / MathF.PI),
                MathF.Atan2(velocityY, velocityX) * (180.0f / MathF.PI),
                0.0f);

            var request = new SpawnRequest(owner.Index, pawn.EntityHandle.Raw, owner.TeamNum, Server.TickCount);
            _pendingSpawns.Enqueue(request);
            try
            {
                _createMolotov!.Invoke(
                    spawnPosition.Handle,
                    angle.Handle,
                    velocity.Handle,
                    velocity.Handle,
                    nint.Zero,
                    46,
                    owner.TeamNum);
                spawnedAny = true;
            }
            catch (Exception exception)
            {
                _pendingSpawns.TryDequeue(out _);
                _plugin.Logger.LogError(exception, "Failed to create a FireRain molotov projectile");
            }
        }

        return spawnedAny;
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity.DesignerName == "molotov_projectile")
        {
            ConfigureMolotov(entity);
            return;
        }

        if (entity.DesignerName == "inferno")
        {
            ConfigureInferno(entity);
        }
    }

    private void ConfigureMolotov(CEntityInstance entity)
    {
        if (!_pendingSpawns.TryPeek(out var request) || request.Tick != Server.TickCount)
        {
            return;
        }

        var molotov = entity.As<CMolotovProjectile>();
        if (molotov is null || !molotov.IsValid || !_pendingSpawns.TryDequeue(out request))
        {
            return;
        }

        molotov.TeamNum = request.Team;
        molotov.Thrower.Raw = request.OwnerHandle;
        molotov.OwnerEntity.Raw = request.OwnerHandle;
        _rainMolotovs[molotov.Index] = (request.OwnerHandle, request.Team);

        Server.NextWorldUpdate(() =>
        {
            if (molotov.IsValid)
            {
                molotov.DetonateTime += 30.0f;
                Utilities.SetStateChanged(molotov, "CBaseGrenade", "m_flDetonateTime");
            }
        });
    }

    private void ConfigureInferno(CEntityInstance entity)
    {
        var inferno = entity.As<CInferno>();
        var source = inferno?.OwnerEntity?.Value;
        if (inferno is null
            || !inferno.IsValid
            || source is null
            || !source.IsValid
            || !_rainMolotovs.TryRemove(source.Index, out var origin))
        {
            return;
        }

        inferno.TeamNum = origin.Team;
        inferno.OwnerEntity.Raw = origin.OwnerHandle;
    }

    private bool TryResolveFactory()
    {
        if (_createMolotov is not null)
        {
            return true;
        }

        try
        {
            _createMolotov = new MemoryFunctionWithReturn<nint, nint, nint, nint, nint, nint, nint, int>(
                GameData.GetSignature("CMolotovProjectile_CreateFunc"));
            return true;
        }
        catch (Exception exception)
        {
            if (!_signatureFailureLogged)
            {
                _signatureFailureLogged = true;
                _plugin.Logger.LogError(exception,
                    "FireRain disabled: CMolotovProjectile_CreateFunc could not be resolved");
            }

            return false;
        }
    }
}
