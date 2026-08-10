using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class NoRecoilSkill : ISkill, ITickSkill
{
    private readonly NoRecoilService _noRecoil;

    public NoRecoilSkill(NoRecoilService noRecoil)
    {
        _noRecoil = noRecoil;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "NoRecoil",
        DisplayName = "专注",
        Description = "射击时无后坐力。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-spread-control",
            "recoil-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        _noRecoil.Acquire(context.Effects);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.Player.PawnIsAlive)
        {
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid)
        {
            return;
        }

        if (pawn.AimPunchServices is not null)
        {
            pawn.AimPunchServices.PredictableBaseTick = 0;
            pawn.AimPunchServices.PredictableBaseTickInterpAmount = 0.0f;
            pawn.AimPunchServices.UnpredictableBaseTick = 0;
        }

        if (pawn.CameraServices is not null)
        {
            pawn.CameraServices.CsViewPunchAngleTick = 0;
            pawn.CameraServices.CsViewPunchAngleTickRatio = 0.0f;
        }
    }
}
