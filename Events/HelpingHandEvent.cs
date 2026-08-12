using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class HelpingHandEvent : RoundEventBase, IRoundEventPreDamage, IRoundEventTick, IRoundEventPlayerJump
{
    private readonly HelpingHandService _service;
    public HelpingHandEvent(HelpingHandService service) => _service = service;
    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "HelpingHand", DisplayName = "助队友一臂之力",
        Description = "攻击队友会让他暂时加速并提高跳跃高度。", DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "friendly-fire-rules" }
    };
    public override void OnApplied(in RoundEventContext context)
    {
        context.Effects.RegisterCleanup(_service.Clear);
        PrintToChatAll("[娱乐事件] 助队友一臂之力：攻击队友会让他加速并跳得更高！");
    }
    public void OnBeforeDamage(in RoundEventContext context, CCSPlayerController victim, CCSPlayerController attacker, CTakeDamageInfo damageInfo)
    { if (victim.Team == attacker.Team) { damageInfo.Damage = 0; _service.Apply(victim); } }
    public void OnTick(in RoundEventContext context) => _service.Tick();
    public void OnPlayerJump(in RoundEventContext context, EventPlayerJump @event)
    { if (@event.Userid is { IsValid: true }) _service.BoostJump(@event.Userid); }
}
