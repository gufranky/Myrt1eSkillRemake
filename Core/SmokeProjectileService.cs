using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Creates real CS2 smoke-grenade projectiles through the same native factory
/// used by jRandomSkills, so spawned smoke detonates and expands normally.
/// </summary>
public sealed class SmokeProjectileService
{
    private static readonly QAngle ZeroAngle = new(0.0f, 0.0f, 0.0f);
    private static readonly Vector ZeroVelocity = new(0.0f, 0.0f, 0.0f);
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private MemoryFunctionWithReturn<nint, nint, nint, nint, nint, int, int, CSmokeGrenadeProjectile>? _createSmoke;
    private bool _signatureFailureLogged;

    public SmokeProjectileService(Myrt1eSkillRemakePlugin plugin) => _plugin = plugin;

    public bool TrySpawn(Vector position, CCSPlayerController owner)
    {
        if (!owner.IsValid || !TryResolveFactory())
        {
            return false;
        }

        try
        {
            _createSmoke!.Invoke(
                position.Handle,
                ZeroAngle.Handle,
                ZeroVelocity.Handle,
                ZeroVelocity.Handle,
                nint.Zero,
                45,
                owner.TeamNum);
            return true;
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(exception, "Failed to create native smoke grenade projectile");
            return false;
        }
    }

    private bool TryResolveFactory()
    {
        if (_createSmoke is not null)
        {
            return true;
        }

        try
        {
            _createSmoke = new MemoryFunctionWithReturn<nint, nint, nint, nint, nint, int, int, CSmokeGrenadeProjectile>(
                GameData.GetSignature("SmokeGrenadeProjectile_CreateFunc"));
            return true;
        }
        catch (Exception exception)
        {
            if (!_signatureFailureLogged)
            {
                _signatureFailureLogged = true;
                _plugin.Logger.LogError(exception,
                    "Native smoke spawning disabled: SmokeGrenadeProjectile_CreateFunc could not be resolved");
            }

            return false;
        }
    }
}
