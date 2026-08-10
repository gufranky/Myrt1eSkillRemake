using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class WallhackSkill : ISkill
{
    private readonly WallhackService _wallhack;

    public WallhackSkill(WallhackService wallhack)
    {
        _wallhack = wallhack;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Wallhack",
        DisplayName = "透视",
        Description = "可以隔着墙壁看到敌人的发光轮廓。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Epic,
        DefaultWeight = 10,
        MaxPerServer = 1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-outline-vision",
            "player-model-control"
        },
        IncompatibleEventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SuperpowerXray",
            "Xray"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        _wallhack.AddViewer(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
        _wallhack.RemoveViewer(context.Player);
    }
}
