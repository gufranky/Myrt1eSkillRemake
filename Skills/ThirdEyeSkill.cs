using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ThirdEyeSkill : ISkill, ITickSkill
{
    private readonly ThirdEyeService _thirdEye;

    public ThirdEyeSkill(ThirdEyeService thirdEye)
    {
        _thirdEye = thirdEye;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "ThirdEye",
        DisplayName = "第三只眼",
        Description = "点击 [css_useSkill] 在第一人称和第三人称视角之间切换。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "camera-view-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var controllerIndex = context.Player.Index;
        context.Effects.RegisterCleanup(() => _thirdEye.Remove(controllerIndex));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!_thirdEye.Toggle(context.Player))
        {
            PluginText.Chat(context.Player, "[第三只眼] 当前无法切换视角。");
        }
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        _thirdEye.Update(context.Player);
    }
}
