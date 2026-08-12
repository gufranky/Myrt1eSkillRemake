using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class HelpingHandService
{
    private sealed class State
    {
        public required float Velocity { get; init; }
        public required float MaxSpeed { get; init; }
        public required DateTime Until { get; set; }
    }

    private readonly HelpingHandSettings _settings;
    private readonly Dictionary<uint, State> _states = new();

    public HelpingHandService(HelpingHandSettings settings) => _settings = settings;

    public void Apply(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        var movement = pawn?.MovementServices;
        if (player is not { IsValid: true, PawnIsAlive: true } || pawn is not { IsValid: true } || movement is null) return;
        if (!_states.TryGetValue(player.Index, out var state))
        {
            state = new State { Velocity = pawn.VelocityModifier, MaxSpeed = movement.Maxspeed, Until = DateTime.MinValue };
            _states[player.Index] = state;
        }

        state.Until = DateTime.UtcNow.AddSeconds(Positive(_settings.DurationSeconds, 5.0f));
        pawn.VelocityModifier = state.Velocity * Positive(_settings.SpeedMultiplier, 1.5f);
        movement.Maxspeed = state.MaxSpeed * Positive(_settings.SpeedMultiplier, 1.5f);
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_pMovementServices");
    }

    public void Tick()
    {
        foreach (var pair in _states.ToArray())
        {
            var player = Utilities.GetPlayerFromIndex((int)pair.Key);
            var pawn = player?.PlayerPawn.Value;
            var movement = pawn?.MovementServices;
            if (player is not { IsValid: true } || pawn is not { IsValid: true } || movement is null) { _states.Remove(pair.Key); continue; }
            if (DateTime.UtcNow >= pair.Value.Until)
            {
                pawn.VelocityModifier = pair.Value.Velocity;
                movement.Maxspeed = pair.Value.MaxSpeed;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_pMovementServices");
                _states.Remove(pair.Key);
            }
        }
    }

    public float JumpMultiplier => Positive(_settings.JumpHeightMultiplier, 1.35f);
    public void Clear() { foreach (var pair in _states.Keys.ToArray()) Restore(pair); }
    public void BoostJump(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (player is not { IsValid: true, PawnIsAlive: true } || pawn is not { IsValid: true }) return;
        pawn.AbsVelocity.Z *= JumpMultiplier;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_vecAbsVelocity");
    }
    private static float Positive(float value, float fallback) => float.IsFinite(value) && value > 0 ? value : fallback;

    private void Restore(uint index)
    {
        var player = Utilities.GetPlayerFromIndex((int)index);
        var pawn = player?.PlayerPawn.Value;
        var movement = pawn?.MovementServices;
        if (_states.TryGetValue(index, out var state) && pawn is { IsValid: true } && movement is not null)
        {
            pawn.VelocityModifier = state.Velocity;
            movement.Maxspeed = state.MaxSpeed;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_pMovementServices");
        }
        _states.Remove(index);
    }
}
