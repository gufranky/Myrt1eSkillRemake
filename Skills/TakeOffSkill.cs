using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class TakeOffSkill : ISkill, IPlayerHurtSkill
{
    private readonly TakeOffSettings _settings;

    public TakeOffSkill(TakeOffSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "TakeOff",
        DisplayName = "🚀 起飞咯",
        Description = "被你击中的敌人将向上跳起来。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "on-hit-knockback-control"
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

    public void OnPlayerHurt(in SkillContext context, EventPlayerHurt @event)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        if ((@event.DmgHealth <= 0 && @event.DmgArmor <= 0)
            || attacker is not { IsValid: true, PawnIsAlive: true }
            || victim is not { IsValid: true, PawnIsAlive: true }
            || attacker.Index != context.Player.Index
            || attacker.Index == victim.Index
            || attacker.Team == victim.Team)
        {
            return;
        }

        var pawn = victim.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        pawn.AbsVelocity.Z = CalculateVerticalVelocity(
            pawn.AbsVelocity.Z,
            _settings.JumpVelocity);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_vecAbsVelocity");
    }

    public static float CalculateVerticalVelocity(float current, float configured)
    {
        var currentVelocity = float.IsFinite(current) ? current : 0.0f;
        var jumpVelocity = float.IsFinite(configured) ? Math.Max(0.0f, configured) : 300.0f;
        return Math.Max(currentVelocity, jumpVelocity);
    }
}
