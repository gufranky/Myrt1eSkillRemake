using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class HeadshotOnlyEvent : RoundEventBase
{
    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "HeadshotOnly",
        DisplayName = "只有爆头",
        Description = "只有爆头命中会造成伤害。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "damage-rules" }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        ConVarOverrides.Set(context.Effects, "mp_damage_headshot_only", true);
        PluginText.ChatAll("[娱乐事件] 只有爆头：身体命中不会造成伤害！");
    }
}
