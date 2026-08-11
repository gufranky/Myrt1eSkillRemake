using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class FalconEyeSkill : ISkill, ITickSkill
{
    private readonly FalconEyeService _falconEye;

    public FalconEyeSkill(FalconEyeService falconEye)
    {
        _falconEye = falconEye;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "FalconEye",
        DisplayName = "🦅 猎鹰眼",
        Description = "点击 [css_useSkill] 在第一人称与鸟瞰摄像头之间切换。",
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
        context.Effects.RegisterCleanup(() => _falconEye.Remove(controllerIndex));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!_falconEye.Toggle(context.Player))
        {
            PluginText.Chat(context.Player, "[猎鹰眼] 当前无法切换鸟瞰视角。");
        }
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        _falconEye.Update(context.Player);
    }
}
