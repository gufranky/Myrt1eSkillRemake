using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class FairDuelEvent : RoundEventBase, IRoundEventPreDamage, IRoundEventTick
{
    private const double InvulnerabilitySeconds = 15.0;
    private bool _active;
    private DateTime _invulnerableUntil;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "FairDuel",
        DisplayName = "公平对决",
        Description = "回合开始后的前 15 秒，双方所有玩家都不会受到伤害。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "damage-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "damage-immunity"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        _invulnerableUntil = DateTime.UtcNow.AddSeconds(InvulnerabilitySeconds);
        context.Effects.RegisterCleanup(() =>
        {
            _active = false;
            _invulnerableUntil = DateTime.MinValue;
        });
        context.Effects.AddTimer((float)InvulnerabilitySeconds, () =>
        {
            if (_active)
            {
                _active = false;
                PrintToChatAll("[娱乐事件] 公平对决：15 秒无敌时间结束！");
            }
        });
        PrintToChatAll("[娱乐事件] 公平对决：双方在 15 秒内都不会受到伤害！");
    }

    public void OnBeforeDamage(
        in RoundEventContext context,
        CCSPlayerController victim,
        CCSPlayerController attacker,
        CTakeDamageInfo damageInfo)
    {
        if (_active && DateTime.UtcNow < _invulnerableUntil && damageInfo.Damage > 0.0f)
        {
            damageInfo.Damage = 0.0f;
        }
    }

    public void OnTick(in RoundEventContext context)
    {
        if (_active && DateTime.UtcNow >= _invulnerableUntil)
        {
            _active = false;
        }
    }
}
