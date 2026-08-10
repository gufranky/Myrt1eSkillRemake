using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class DashSkill : ISkill, ITickSkill
{
    private sealed class DashState
    {
        public bool CanUse { get; set; } = true;
        public DateTime CooldownStartedAt { get; set; } = DateTime.MinValue;
        public int Jumps { get; set; }
        public bool WasOnGround { get; set; } = true;
        public int JumpReleasedTicks { get; set; } = 10;
    }

    private readonly DashSettings _settings;

    public DashSkill(DashSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Dash",
        DisplayName = "冲刺",
        Description = "在空中再次按下跳跃键即可向移动方向冲刺。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "jump-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new DashState());
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.Player.PawnIsAlive || !context.State.TryGet<DashState>(out var state))
        {
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid)
        {
            return;
        }

        var jumpPressed = context.Player.Buttons.HasFlag(PlayerButtons.Jump)
            || (pawn.MovementServices?.QueuedButtonChangeMask & (ulong)PlayerButtons.Jump) != 0;
        var isOnGround = pawn.GroundEntity is { IsValid: true };

        if (!state.CanUse && DateTime.UtcNow >= state.CooldownStartedAt.AddSeconds(GetCooldownSeconds()))
        {
            state.CanUse = true;
        }

        if (state.WasOnGround && !isOnGround && jumpPressed)
        {
            state.Jumps = 0;
        }
        else if (isOnGround)
        {
            state.Jumps = 0;
        }
        else if (jumpPressed && state.Jumps < 1 && state.JumpReleasedTicks >= 3 && state.CanUse)
        {
            Dash(context.Player, pawn, state);
        }

        state.WasOnGround = isOnGround;
        state.JumpReleasedTicks = jumpPressed ? 0 : state.JumpReleasedTicks + 1;
    }

    private void Dash(CCSPlayerController player, CCSPlayerPawn pawn, DashState state)
    {
        state.Jumps++;
        state.CanUse = false;
        state.CooldownStartedAt = DateTime.UtcNow;

        var moveX = 0.0f;
        var moveY = 1.0f;
        if (_settings.AnyDirection)
        {
            moveY = 0.0f;
            if (player.Buttons.HasFlag(PlayerButtons.Forward)) moveY += 1.0f;
            if (player.Buttons.HasFlag(PlayerButtons.Back)) moveY -= 1.0f;
            if (player.Buttons.HasFlag(PlayerButtons.Moveleft)) moveX += 1.0f;
            if (player.Buttons.HasFlag(PlayerButtons.Moveright)) moveX -= 1.0f;
            if (moveX == 0.0f && moveY == 0.0f) moveY = 1.0f;
        }

        var moveAngle = MathF.Atan2(moveX, moveY);
        var yaw = pawn.EyeAngles.Y * (MathF.PI / 180.0f) + moveAngle;
        var pushVelocity = float.IsFinite(_settings.PushVelocity)
            ? Math.Max(0.0f, _settings.PushVelocity)
            : 600.0f;
        var jumpVelocity = float.IsFinite(_settings.JumpVelocity)
            ? _settings.JumpVelocity
            : 150.0f;

        pawn.AbsVelocity.X = MathF.Cos(yaw) * pushVelocity;
        pawn.AbsVelocity.Y = MathF.Sin(yaw) * pushVelocity;
        pawn.AbsVelocity.Z += jumpVelocity;
        pawn.EmitSound("Default.WalkJump", volume: Math.Clamp(_settings.SoundVolume, 0.0f, 1.0f));
    }

    private double GetCooldownSeconds()
    {
        return float.IsFinite(_settings.CooldownSeconds)
            ? Math.Max(0.0f, _settings.CooldownSeconds)
            : 2.0f;
    }
}
