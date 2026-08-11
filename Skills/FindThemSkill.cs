using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class FindThemSkill : ISkill, ITickSkill, IPlayerDeathSkill
{
    private readonly FindThemService _scouts;

    public FindThemSkill(FindThemSettings settings, FindThemService scouts)
    {
        _scouts = scouts;
        Descriptor = new SkillDescriptor
        {
            Id = "FindThem",
            DisplayName = "🐔 找到他们",
            Description = "点击 [css_useskill] 为每名存活敌人派出一只追踪鸡；跟着鸡就能找到对应敌人。",
            Kind = SkillKind.Active,
            Rarity = SkillRarity.Rare,
            DefaultWeight = 10,
            MaxPerServer = 1,
            CooldownSeconds = PositiveFiniteOr(settings.CooldownSeconds, 30.0f),
            ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "tracking-companion-control"
            }
        };
    }

    public SkillDescriptor Descriptor { get; }

    public void OnGranted(in SkillContext context)
    {
        var ownerIndex = context.Player.Index;
        context.Effects.RegisterCleanup(() => _scouts.Remove(ownerIndex));
    }

    public void OnActivated(in SkillContext context)
    {
        var count = _scouts.Deploy(context.Player);
        if (count == 0)
        {
            PluginText.Chat(context.Player, "[找到他们] 当前没有可追踪的存活敌人。");
            return;
        }

        PluginText.Chat(context.Player, $"[找到他们] 已派出 {count} 只追踪鸡，跟着它们寻找敌人！");
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context) => _scouts.Update(context.Player);

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid is { IsValid: true } victim && victim.Slot == context.Player.Slot)
        {
            _scouts.Remove(context.Player.Index);
        }
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;
}
