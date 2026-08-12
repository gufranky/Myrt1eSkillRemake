using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class HelpingHandSkill : ISkill, IPreDamageAttackerSkill, ITickSkill, IPlayerJumpSkill
{
    private readonly HelpingHandService _service;
    public HelpingHandSkill(HelpingHandService service) => _service = service;
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "HelpingHand", DisplayName = "助队友一臂之力", Description = "攻击队友会让他加速并提高跳跃高度。",
        Kind = SkillKind.Passive, Rarity = SkillRarity.Common, DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "friendly-fire-rules" }
    };
    public void OnGranted(in SkillContext context) { }
    public void OnActivated(in SkillContext context) { }
    public void OnRevoked(in SkillContext context) { }
    public void OnBeforeDamageDealt(in SkillContext context, CCSPlayerController victim, CTakeDamageInfo damageInfo)
    { if (victim.Team == context.Player.Team) { damageInfo.Damage = 0; _service.Apply(victim); } }
    public void OnTick(in SkillContext context) => _service.Tick();
    public void OnPlayerJump(in SkillContext context, EventPlayerJump @event)
    { if (@event.Userid is { IsValid: true }) _service.BoostJump(@event.Userid); }
}
