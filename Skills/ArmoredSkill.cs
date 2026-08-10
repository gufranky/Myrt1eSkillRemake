using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ArmoredSkill : ISkill, IPreDamageSkill
{
    private const float DefaultMinimumMultiplier = 0.65f;
    private const float DefaultMaximumMultiplier = 0.85f;
    private sealed record ArmoredState(float DamageTakenMultiplier);

    private readonly ArmoredSettings _settings;

    public ArmoredSkill(ArmoredSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Armored",
        DisplayName = "覆甲",
        Description = "受到的伤害会乘以一个随机倍率。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "damage-taken-multiplier"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var configuredMinimum = float.IsFinite(_settings.MinimumDamageMultiplier)
            ? _settings.MinimumDamageMultiplier
            : DefaultMinimumMultiplier;
        var configuredMaximum = float.IsFinite(_settings.MaximumDamageMultiplier)
            ? _settings.MaximumDamageMultiplier
            : DefaultMaximumMultiplier;
        var minimum = Math.Clamp(Math.Min(configuredMinimum, configuredMaximum), 0.0f, 10.0f);
        var maximum = Math.Clamp(Math.Max(configuredMinimum, configuredMaximum), minimum, 10.0f);
        var multiplier = minimum + Random.Shared.NextSingle() * (maximum - minimum);

        context.State.Set(new ArmoredState(multiplier));
        PluginText.Chat(context.Player, $"[随机技能] 覆甲：你的受伤倍率为 {multiplier:0.00}x");
    }

    public void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo)
    {
        if (damageInfo.Damage <= 0.0f || !context.State.TryGet<ArmoredState>(out var state))
        {
            return;
        }

        damageInfo.Damage *= state.DamageTakenMultiplier;
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
