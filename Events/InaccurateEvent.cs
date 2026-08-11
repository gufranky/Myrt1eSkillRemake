using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class InaccurateEvent : RoundEventBase
{
    private readonly InaccurateSettings _settings;

    public InaccurateEvent(InaccurateSettings settings)
    {
        _settings = settings;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "Inaccurate",
        DisplayName = "🎯 全员马枪",
        Description = "所有武器的弹道扩散大幅增加，子弹很难命中准星位置！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-spread-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-spread-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        var forcedSpread = float.IsFinite(_settings.ForcedSpread)
            ? Math.Max(0.0f, _settings.ForcedSpread)
            : 0.088f;

        // Explicitly disable no-spread before forcing the server-authoritative
        // spread value. EffectScope restores both original values on removal.
        ConVarOverrides.Set(context.Effects, "weapon_accuracy_nospread", false);
        ConVarOverrides.Set(context.Effects, "weapon_accuracy_forcespread", forcedSpread);
        PrintToChatAll($"[娱乐事件] 🎯 全员马枪：全体武器强制扩散 {forcedSpread:0.###}，子弹很难打准！");
    }
}
