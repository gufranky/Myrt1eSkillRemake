using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Myrt1eSkill_Remake.Core;

public sealed class NinjaVisibilityService : IDisposable
{
    public const float FullInvisibilityThreshold = 0.90f;

    private sealed class NinjaState
    {
        public required Color OriginalRender { get; init; }
        public float Invisibility { get; set; } = -1.0f;
        public bool FullyInvisible { get; set; }
    }

    private readonly Dictionary<uint, NinjaState> _states = new();
    private bool _disposed;

    public bool Acquire(CCSPlayerController player, EffectScope effects)
    {
        if (_disposed || player is not { IsValid: true, PawnIsAlive: true })
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return false;
        }

        Remove(player.Index, player);
        _states[player.Index] = new NinjaState { OriginalRender = pawn.Render };
        var playerIndex = player.Index;
        effects.RegisterCleanup(() => Remove(playerIndex));
        return true;
    }

    public void SetInvisibility(CCSPlayerController player, float invisibility)
    {
        if (!_states.TryGetValue(player.Index, out var state))
        {
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        var safeInvisibility = float.IsFinite(invisibility)
            ? Math.Clamp(invisibility, 0.0f, 1.0f)
            : 0.0f;
        var fullyInvisible = safeInvisibility >= FullInvisibilityThreshold;
        if (Math.Abs(state.Invisibility - safeInvisibility) < 0.001f
            && state.FullyInvisible == fullyInvisible)
        {
            return;
        }

        state.Invisibility = safeInvisibility;
        state.FullyInvisible = fullyInvisible;
        var alpha = Math.Clamp(255 - (int)(255 * safeInvisibility), 0, 255);
        pawn.Render = Color.FromArgb(
            alpha,
            state.OriginalRender.R,
            state.OriginalRender.G,
            state.OriginalRender.B);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    public void Remove(uint playerIndex, CCSPlayerController? player = null)
    {
        if (!_states.Remove(playerIndex, out var state))
        {
            return;
        }

        player ??= Utilities.GetPlayerFromIndex((int)playerIndex);
        var pawn = player?.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        pawn.Render = state.OriginalRender;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    public void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_states.Count == 0)
        {
            return;
        }

        var hidden = _states
            .Where(entry => entry.Value.FullyInvisible)
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
            foreach (var ninja in hidden)
            {
                if (viewer.Index == ninja.Index || viewer.Team == ninja.Team)
                {
                    continue;
                }

                var pawn = ninja.PlayerPawn.Value;
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

        foreach (var playerIndex in _states.Keys.ToArray())
        {
            Remove(playerIndex);
        }

        _disposed = true;
    }

    private static void HideCarriedEntities(CCheckTransmitInfo info, CCSPlayerPawn pawn)
    {
        var weapons = pawn.WeaponServices;
        if (weapons is null)
        {
            return;
        }

        if (weapons.ActiveWeapon.Value is { IsValid: true } activeWeapon)
        {
            info.TransmitEntities.Remove(activeWeapon.Index);
        }

        foreach (var weaponHandle in weapons.MyWeapons)
        {
            if (weaponHandle.Value is { IsValid: true } weapon)
            {
                info.TransmitEntities.Remove(weapon.Index);
            }
        }
    }
}
