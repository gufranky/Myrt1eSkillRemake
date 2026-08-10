using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ExplodingBarrelSkill : ISkill
{
    private readonly ExplodingBarrelService _barrels;

    public ExplodingBarrelSkill(ExplodingBarrelService barrels)
    {
        _barrels = barrels;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "ExplodingBarrel",
        DisplayName = "爆炸桶",
        Description = "按下 [css_useskill] 放置一个被射击时会爆炸的桶。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = 2,
        CooldownSeconds = 20.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "world-prop-placement"
        }
    };

    public void OnGranted(in SkillContext context)
    {
    }

    public void OnActivated(in SkillContext context)
    {
        if (!_barrels.Place(context.Player, context.Effects))
        {
            PluginText.Chat(context.Player, "[爆炸桶] 当前无法放置爆炸桶。");
        }
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
