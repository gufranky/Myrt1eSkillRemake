using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class KamikazeChickenSkill : ISkill, ITickSkill, IPlayerDeathSkill
{
    private readonly KamikazeChickenService _chickens;

    public KamikazeChickenSkill(KamikazeChickenSettings settings, KamikazeChickenService chickens)
    {
        _chickens = chickens;
        Descriptor = new SkillDescriptor
        {
            Id = "KamikazeChicken",
            DisplayName = "💥 自爆小鸡",
            Description = "点击 [css_useskill] 放出一只红色自爆鸡；它会随机追踪一名敌人，靠近后召唤 HE 爆炸。小鸡只有 1 点生命。",
            Kind = SkillKind.Active,
            Rarity = SkillRarity.Rare,
            DefaultWeight = 10,
            MaxPerServer = 1,
            CooldownSeconds = PositiveFiniteOr(settings.CooldownSeconds, 30.0f),
            ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "tracking-companion-control",
                "explosive-companion-control"
            }
        };
    }

    public SkillDescriptor Descriptor { get; }

    public void OnGranted(in SkillContext context)
    {
        var ownerIndex = context.Player.Index;
        context.Effects.RegisterCleanup(() => _chickens.Remove(ownerIndex));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!_chickens.Deploy(context.Player))
        {
            PluginText.Chat(context.Player, "[自爆小鸡] 当前没有可追踪的存活敌人。");
            return;
        }

        PluginText.Chat(context.Player, "[自爆小鸡] 已锁定一名随机敌人。小鸡只有 1 HP，爆炸时注意保持距离！");
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context) => _chickens.Update(context.Player);

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid is { IsValid: true } victim && victim.Slot == context.Player.Slot)
        {
            _chickens.Remove(context.Player.Index);
        }
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;
}
