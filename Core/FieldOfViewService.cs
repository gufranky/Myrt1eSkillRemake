using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Coordinates temporary DesiredFOV overrides and restores the value that was
/// present before the first effect acquired the player.
/// </summary>
public sealed class FieldOfViewService : IDisposable
{
    private sealed class TargetState
    {
        public required uint OriginalFov { get; init; }
        public Dictionary<string, uint> Owners { get; } = new(StringComparer.Ordinal);
    }

    private readonly Dictionary<uint, TargetState> _targets = new();

    public bool Apply(CCSPlayerController player, string owner, uint fov)
    {
        if (!player.IsValid || string.IsNullOrWhiteSpace(owner))
        {
            return false;
        }

        if (!_targets.TryGetValue(player.Index, out var state))
        {
            state = new TargetState { OriginalFov = player.DesiredFOV };
            _targets[player.Index] = state;
        }

        state.Owners[owner] = fov;
        Set(player, fov);
        return true;
    }

    public bool Release(uint playerIndex, string owner)
    {
        if (!_targets.TryGetValue(playerIndex, out var state) || !state.Owners.Remove(owner))
        {
            return false;
        }

        var player = Utilities.GetPlayerFromIndex((int)playerIndex);
        if (state.Owners.Count > 0)
        {
            if (player is { IsValid: true })
            {
                Set(player, state.Owners.Values.Last());
            }

            return true;
        }

        _targets.Remove(playerIndex);
        if (player is { IsValid: true })
        {
            Set(player, state.OriginalFov);
        }

        return true;
    }

    public void Dispose()
    {
        foreach (var pair in _targets.ToArray())
        {
            var player = Utilities.GetPlayerFromIndex((int)pair.Key);
            if (player is { IsValid: true })
            {
                Set(player, pair.Value.OriginalFov);
            }
        }

        _targets.Clear();
    }

    private static void Set(CCSPlayerController player, uint fov)
    {
        player.DesiredFOV = fov;
        Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
    }
}
