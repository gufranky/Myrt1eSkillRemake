using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class IllusionistSkill : ISkill
{
    private readonly IllusionistService _illusionist;

    public IllusionistSkill(IllusionistService illusionist)
    {
        _illusionist = illusionist;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Illusionist",
        DisplayName = "🎭 魔术师",
        Description = "点击 css_useskill 部署直线前行的复制品；射击它的敌人受到 20 点伤害。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = 2,
        CooldownSeconds = 30.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "world-prop-placement"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        PluginText.Chat(context.Player, "[魔术师] 点击 css_useskill 部署一个向前移动的复制品。");
    }

    public void OnActivated(in SkillContext context)
    {
        if (!_illusionist.Deploy(context.Player, context.Effects))
        {
            PluginText.Chat(context.Player, "[魔术师] 当前无法部署复制品。");
        }
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
