using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class SilentWorldEvent : RoundEventBase
{
    private const string Owner = "Event:SilentWorld";
    private readonly DeafSoundService _sounds;

    public SilentWorldEvent(DeafSoundService sounds)
    {
        _sounds = sounds;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "SilentWorld",
        DisplayName = "🔇 无声世界",
        Description = "所有玩家都听不到服务器发送的游戏声音！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "global-sound-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "targeted-sound-debuff",
            "personal-sound-suppression"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _sounds.MuteAll(Owner);
        context.Effects.RegisterCleanup(() => _sounds.ReleaseAll(Owner));
        PrintToChatAll("[娱乐事件] 🔇 无声世界：全员失去听觉，枪声、脚步与爆炸声全部消失！");
    }
}
