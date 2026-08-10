using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Myrt1eSkill_Remake.Core;

public sealed class GhostService : IDisposable
{
    private sealed class GhostState
    {
        public required Color OriginalRender { get; init; }
        public bool Invisible { get; set; } = true;
    }

    private readonly Dictionary<uint, GhostState> _states = new();
    private bool _disposed;

    public bool Hide(CCSPlayerController player)
    {
        if (_disposed || !player.IsValid || !player.PawnIsAlive)
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return false;
        }

        Remove(player.Index, player);
        _states[player.Index] = new GhostState { OriginalRender = pawn.Render };
        pawn.Render = Color.FromArgb(128, pawn.Render.R, pawn.Render.G, pawn.Render.B);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        return true;
    }

    public bool Reveal(CCSPlayerController player)
    {
        if (!_states.TryGetValue(player.Index, out var state) || !state.Invisible)
        {
            return false;
        }

        state.Invisible = false;
        RestoreRender(player, state);
        return true;
    }

    public void Remove(uint controllerIndex, CCSPlayerController? player = null)
    {
        if (!_states.Remove(controllerIndex, out var state))
        {
            return;
        }

        player ??= Utilities.GetPlayerFromIndex((int)controllerIndex);
        if (player is { IsValid: true })
        {
            RestoreRender(player, state);
        }
    }

    public void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_states.Count == 0)
        {
            return;
        }

        var hidden = _states
            .Where(entry => entry.Value.Invisible)
            .Select(entry => Utilities.GetPlayerFromIndex((int)entry.Key))
            .Where(player => player is { IsValid: true, PawnIsAlive: true })
            .Cast<CCSPlayerController>()
            .ToArray();
        if (hidden.Length == 0)
        {
            return;
        }

        foreach (var (info, viewer) in infoList)
        {
            if (viewer is not { IsValid: true } || viewer.Team == CsTeam.Spectator)
            {
                continue;
            }

            var observedHandle = viewer.Pawn.Value?.ObserverServices?.ObserverTarget?.Value?.Handle ?? nint.Zero;
            foreach (var ghost in hidden)
            {
                if (viewer.Index == ghost.Index || viewer.Team == ghost.Team)
                {
                    continue;
                }

                var pawn = ghost.PlayerPawn.Value;
                if (pawn is not { IsValid: true }
                    || (observedHandle != nint.Zero && observedHandle == pawn.Handle))
                {
                    continue;
                }

                info.TransmitEntities.Remove(pawn.Index);
                HideCarriedEntities(info, pawn);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var controllerIndex in _states.Keys.ToArray())
        {
            Remove(controllerIndex);
        }

        _disposed = true;
    }

    private static void HideCarriedEntities(CCheckTransmitInfo info, CCSPlayerPawn pawn)
    {
        var weaponServices = pawn.WeaponServices;
        if (weaponServices is null)
        {
            return;
        }

        var activeWeapon = weaponServices.ActiveWeapon.Value;
        if (activeWeapon is { IsValid: true })
        {
            info.TransmitEntities.Remove(activeWeapon.Index);
        }

        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Value;
            if (weapon is { IsValid: true })
            {
                info.TransmitEntities.Remove(weapon.Index);
            }
        }
    }

    private static void RestoreRender(CCSPlayerController player, GhostState state)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        pawn.Render = state.OriginalRender;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
}
