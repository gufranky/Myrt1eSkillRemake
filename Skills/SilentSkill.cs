using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class SilentSkill : ISkill
{
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Silent",
        DisplayName = "🤫 静默",
        Description = "你的脚步、跳跃和落地声不会被其他玩家听见。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "personal-sound-suppression"
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
}
