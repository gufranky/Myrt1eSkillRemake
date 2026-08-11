using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class AimbotSkill : ISkill, IPreDamageAttackerSkill
{
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Aimbot",
        DisplayName = "🎯 自瞄",
        Description = "每一颗击中的子弹都算作爆头。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bullet-hitgroup-override"
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
            && victim.PawnIsAlive)
        {
            AimbotHitGroupService.TryForceHeadshot(damageInfo);
        }
    }
}
