using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace Myrt1eSkill_Remake.Core;

public sealed class PlayerViewService
{
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private MemoryFunctionVoid<CBasePlayerPawn, QAngle>? _snapViewAngles;
    private bool _failureLogged;

    public PlayerViewService(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
    }

    public bool TrySet(CBasePlayerPawn pawn, QAngle angle)
    {
        try
        {
            _snapViewAngles ??= new MemoryFunctionVoid<CBasePlayerPawn, QAngle>(
                GameData.GetSignature("SnapViewAngles"));
            _snapViewAngles.Invoke(pawn, angle);
            return true;
        }
        catch (Exception exception)
        {
            if (!_failureLogged)
            {
                _failureLogged = true;
                _plugin.Logger.LogError(exception, "SnapViewAngles could not be resolved");
            }

            return false;
        }
    }
}
