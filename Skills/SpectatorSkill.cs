using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class SpectatorSkill : ISkill, ITickSkill
{
    private readonly SpectatorCameraService _spectator;

    public SpectatorSkill(SpectatorCameraService spectator)
    {
        _spectator = spectator;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Spectator",
        DisplayName = "👁 观察者",
        Description = "点击 [css_useSkill] 旁观一个随机敌人。",
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
        var viewerIndex = context.Player.Index;
        context.Effects.RegisterCleanup(() => _spectator.Remove(viewerIndex));
    }

    public void OnActivated(in SkillContext context)
    {
        var result = _spectator.Toggle(context.Player, out var target);
        switch (result)
        {
            case SpectatorToggleResult.Started:
                PluginText.Chat(context.Player,
                    $"[观察者] 正在旁观 {target?.PlayerName ?? "随机敌人"}；再次使用技能可返回。");
                break;
            case SpectatorToggleResult.Stopped:
                PluginText.Chat(context.Player, "[观察者] 已返回自己的视角。");
                break;
            default:
                PluginText.Chat(context.Player, "[观察者] 当前没有可旁观的存活敌人。");
                break;
        }
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        _spectator.Update(context.Player);
    }
}
