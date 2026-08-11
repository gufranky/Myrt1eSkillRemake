using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Rewrites the four movement button bits and both movement-axis pairs during
/// player pre-think. Modern CS2 movement consumes the float axes as well as the
/// legacy button mask, so changing only CInButtonState has no visible effect.
/// Attack, jump, duck, use and view input are deliberately left untouched.
/// </summary>
public sealed class MindHackService
{
    private sealed class TargetState
    {
        public required ulong SteamId { get; init; }
        public HashSet<string> Owners { get; } = new(StringComparer.Ordinal);
    }

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly Dictionary<uint, TargetState> _targets = new();
    private readonly Dictionary<string, uint> _ownerTargets = new(StringComparer.Ordinal);
    private bool _loaded;

    public MindHackService(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
    }

    public void Load()
    {
        if (_loaded)
        {
            return;
        }

        _plugin.RegisterListener<Listeners.OnPlayerButtonsChanged>(OnPlayerButtonsChanged);
        _loaded = true;
    }

    public void Unload()
    {
        if (_loaded)
        {
            _plugin.RemoveListener<Listeners.OnPlayerButtonsChanged>(OnPlayerButtonsChanged);
        }

        foreach (var targetIndex in _targets.Keys.ToArray())
        {
            RestoreCurrentMovement(targetIndex);
        }

        _targets.Clear();
        _ownerTargets.Clear();
        _loaded = false;
    }

    public bool Apply(CCSPlayerController target, string owner)
    {
        if (!target.IsValid
            || !target.PawnIsAlive
            || string.IsNullOrWhiteSpace(owner)
            || target.PlayerPawn.Value?.MovementServices?.Buttons is null)
        {
            return false;
        }

        Release(owner);
        if (_targets.TryGetValue(target.Index, out var state)
            && state.SteamId != target.SteamID)
        {
            foreach (var staleOwner in state.Owners)
            {
                _ownerTargets.Remove(staleOwner);
            }

            _targets.Remove(target.Index);
            state = null;
        }

        if (state is null)
        {
            state = new TargetState { SteamId = target.SteamID };
            _targets[target.Index] = state;
        }

        var firstOwner = state.Owners.Count == 0;
        state.Owners.Add(owner);
        _ownerTargets[owner] = target.Index;
        if (firstOwner)
        {
            RewriteCurrentMovement(target);
        }
        return true;
    }

    public bool Release(string owner)
    {
        if (!_ownerTargets.Remove(owner, out var targetIndex)
            || !_targets.TryGetValue(targetIndex, out var state))
        {
            return false;
        }

        state.Owners.Remove(owner);
        if (state.Owners.Count == 0)
        {
            _targets.Remove(targetIndex);
            RestoreCurrentMovement(targetIndex);
        }

        return true;
    }

    public bool IsAffected(CCSPlayerController player) =>
        player.IsValid
        && _targets.TryGetValue(player.Index, out var state)
        && state.SteamId == player.SteamID
        && state.Owners.Count > 0;

    public static PlayerButtons ReverseMovementButtons(PlayerButtons buttons)
    {
        const PlayerButtons movementMask = PlayerButtons.Forward
                                           | PlayerButtons.Back
                                           | PlayerButtons.Moveleft
                                           | PlayerButtons.Moveright;
        var reversed = buttons & ~movementMask;

        if (buttons.HasFlag(PlayerButtons.Forward))
        {
            reversed |= PlayerButtons.Back;
        }

        if (buttons.HasFlag(PlayerButtons.Back))
        {
            reversed |= PlayerButtons.Forward;
        }

        if (buttons.HasFlag(PlayerButtons.Moveleft))
        {
            reversed |= PlayerButtons.Moveright;
        }

        if (buttons.HasFlag(PlayerButtons.Moveright))
        {
            reversed |= PlayerButtons.Moveleft;
        }

        return reversed;
    }

    public static float ReverseMovementAxis(float axis) =>
        float.IsFinite(axis) ? -axis : 0.0f;

    private void OnPlayerButtonsChanged(
        CCSPlayerController player,
        PlayerButtons pressed,
        PlayerButtons released)
    {
        if (!IsAffected(player))
        {
            return;
        }

        RewriteCurrentMovement(player);
    }

    private static void RewriteCurrentMovement(CCSPlayerController player)
    {
        var movement = player.PlayerPawn.Value?.MovementServices;
        if (movement is null)
        {
            return;
        }

        var buttonState = movement.Buttons;
        var current = (PlayerButtons)buttonState.ButtonStates[0];
        buttonState.ButtonStates[0] = (ulong)ReverseMovementButtons(current);

        movement.CmdForwardMove = ReverseMovementAxis(movement.CmdForwardMove);
        movement.CmdLeftMove = ReverseMovementAxis(movement.CmdLeftMove);
        movement.ForwardMove = ReverseMovementAxis(movement.ForwardMove);
        movement.LeftMove = ReverseMovementAxis(movement.LeftMove);
    }

    private static void RestoreCurrentMovement(uint targetIndex)
    {
        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is { IsValid: true })
        {
            RewriteCurrentMovement(target);
        }
    }
}
