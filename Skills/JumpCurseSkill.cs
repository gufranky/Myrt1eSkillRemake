using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class JumpCurseSkill : ISkill, IPlayerJumpSkill
{
    private readonly JumpCurseSettings _settings;

    public JumpCurseSkill(JumpCurseSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "JumpCurse",
        DisplayName = "🦘 Jump Curse",
        Description = "你跳跃时，所有仍在地面的存活敌人都会同时跳跃。",
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
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerJump(in SkillContext context, EventPlayerJump @event)
    {
        var jumper = @event.Userid;
        if (jumper is not { IsValid: true, PawnIsAlive: true }
            || jumper.Index != context.Player.Index
            || jumper.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
        {
            return;
        }

        var jumpVelocity = float.IsFinite(_settings.JumpVelocity)
            ? Math.Max(0.0f, _settings.JumpVelocity)
            : 301.0f;
        foreach (var enemy in Utilities.GetPlayers())
        {
            if (!enemy.IsValid
                || !enemy.PawnIsAlive
                || enemy.Team == jumper.Team
                || enemy.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
            {
                continue;
            }

            var pawn = enemy.PlayerPawn.Value;
            if (pawn is not { IsValid: true }
                || (pawn.Flags & (uint)PlayerFlags.FL_ONGROUND) == 0)
            {
                continue;
            }

            pawn.AbsVelocity.Z = jumpVelocity;
        }
    }
}
