using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class JumperSkill : ISkill, ITickSkill
{
    private sealed class JumperState
    {
        public int ExtraJumpsUsed { get; set; }
        public bool WasJumpDown { get; set; }
    }

    private readonly JumperSettings _settings;

    public JumperSkill(JumperSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Jumper",
        DisplayName = "🦘 跳跃者",
        Description = "你可以在空中额外跳跃一次。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "jump-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new JumperState());
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.Player.PawnIsAlive
            || !context.State.TryGet<JumperState>(out var state))
        {
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        var movement = pawn.MovementServices;
        var isJumpDown = context.Player.Buttons.HasFlag(PlayerButtons.Jump)
            || (movement?.Buttons?.ButtonStates[0] & (ulong)PlayerButtons.Jump) != 0;
        var jumpPressed = (isJumpDown && !state.WasJumpDown)
            || (movement?.QueuedButtonChangeMask & (ulong)PlayerButtons.Jump) != 0;
        var isOnGround = (pawn.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0;

        if (isOnGround)
        {
            state.ExtraJumpsUsed = 0;
        }
        else if (jumpPressed && state.ExtraJumpsUsed < 1)
        {
            state.ExtraJumpsUsed++;
            pawn.AbsVelocity.Z = PositiveFiniteOr(_settings.JumpVelocity, 300.0f);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_vecAbsVelocity");
            pawn.EmitSound(
                "Default.WalkJump",
                volume: Math.Clamp(FiniteOr(_settings.SoundVolume, 1.0f), 0.0f, 1.0f));
        }

        state.WasJumpDown = isJumpDown;
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static float FiniteOr(float value, float fallback) =>
        float.IsFinite(value) ? value : fallback;
}
