using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class IlliterateSkill : ISkill
{
    private readonly IlliterateService _illiterate;

    public IlliterateSkill(IlliterateService illiterate)
    {
        _illiterate = illiterate;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Illiterate",
        DisplayName = "文盲",
        Description = "只要你还活着，敌人就无法正常阅读插件文字。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = 1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "plugin-text-distortion"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        _illiterate.AddHolder(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
        _illiterate.RemoveHolder(context.Player);
    }
}
