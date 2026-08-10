using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class OneShotSkill : ISkill, IPreDamageAttackerSkill
{
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "OneShot",
        DisplayName = "一次性",
        Description = "击中敌人会立即将其杀死。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "enemy-damage-override"
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

    public void OnBeforeDamageDealt(
        in SkillContext context,
        CCSPlayerController victim,
        CTakeDamageInfo damageInfo)
    {
        if (victim.IsValid
            && victim.PawnIsAlive
            && victim.Team != context.Player.Team
            && damageInfo.Damage > 0.0f)
        {
            damageInfo.Damage = 1000.0f;
        }
    }
}
