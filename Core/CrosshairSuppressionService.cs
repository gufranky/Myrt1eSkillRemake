using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Coordinates temporary crosshair suppression so independent effects cannot
/// accidentally restore a crosshair that is still owned by another effect.
/// </summary>
public sealed class CrosshairSuppressionService : IDisposable
{
    public const uint CrosshairHideHudBit = 1u << 8;

    private readonly Dictionary<uint, HashSet<string>> _owners = new();

    public bool Hide(CCSPlayerController player, string owner)
    {
        if (!player.IsValid || string.IsNullOrWhiteSpace(owner))
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return false;
        }

        if (!_owners.TryGetValue(player.Index, out var owners))
        {
            owners = new HashSet<string>(StringComparer.Ordinal);
            _owners[player.Index] = owners;
        }

        owners.Add(owner);
        Apply(pawn, hidden: true);
        return true;
    }

    public bool Release(uint playerIndex, string owner)
    {
        if (!_owners.TryGetValue(playerIndex, out var owners) || !owners.Remove(owner))
        {
            return false;
        }

        var hidden = owners.Count > 0;
        if (!hidden)
        {
            _owners.Remove(playerIndex);
        }

        var player = Utilities.GetPlayerFromIndex((int)playerIndex);
        var pawn = player?.PlayerPawn.Value;
        if (pawn is { IsValid: true })
        {
            Apply(pawn, hidden);
        }

        return true;
    }

    public void ClearPlayer(CCSPlayerController? player)
    {
        if (player is null)
        {
            return;
        }

        _owners.Remove(player.Index);
        var pawn = player.PlayerPawn.Value;
        if (pawn is { IsValid: true })
        {
            Apply(pawn, hidden: false);
        }
    }

    public void Dispose()
    {
        foreach (var playerIndex in _owners.Keys.ToArray())
        {
            var player = Utilities.GetPlayerFromIndex((int)playerIndex);
            var pawn = player?.PlayerPawn.Value;
            if (pawn is { IsValid: true })
            {
                Apply(pawn, hidden: false);
            }
        }

        _owners.Clear();
    }

    private static void Apply(CCSPlayerPawn pawn, bool hidden)
    {
        if (hidden)
        {
            pawn.HideHUD |= CrosshairHideHudBit;
        }
        else
        {
            pawn.HideHUD &= ~CrosshairHideHudBit;
        }

        Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_iHideHUD");
    }
}
