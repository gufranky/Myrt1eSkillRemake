using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class FortniteSkill : ISkill
{
    private readonly FortniteService _walls;

    public FortniteSkill(FortniteService walls)
    {
        _walls = walls;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Fortnite",
        DisplayName = "堡垒之夜",
        Description = "点击 [css_useskill] 创建可破坏的路障。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = 5,
        CooldownSeconds = 2.0f,
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
        if (!_walls.Place(context.Player, context.Effects))
        {
            PluginText.Chat(context.Player, "[堡垒之夜] 当前无法放置路障。");
        }
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
