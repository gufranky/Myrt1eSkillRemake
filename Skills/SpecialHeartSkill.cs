using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class SpecialHeartSkill : ISkill, IPreDamageSkill, ITickSkill, IPlayerDeathSkill
{
    private readonly SpecialHeartCompanionService _companions;
    public SpecialHeartSkill(SpecialHeartSettings settings, SpecialHeartCompanionService companions) => _companions = companions;

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "SpecialHeart", DisplayName = "特殊心脏",
        Description = "你无敌，但专属蓝色小鸡死亡时你也会死亡；小鸡体型 3 倍并高速跟随。",
        Kind = SkillKind.Passive, Rarity = SkillRarity.Legendary, DefaultWeight = 10, MaxPerServer = 1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tracking-companion-control", "damage-immunity" }
    };

    public void OnGranted(in SkillContext context)
    {
        if (!_companions.Spawn(context.Player)) throw new InvalidOperationException("SpecialHeart chicken spawn failed.");
        var ownerIndex = context.Player.Index;
        context.Effects.RegisterCleanup(() => _companions.Remove(ownerIndex));
        PluginText.Chat(context.Player, "[特殊心脏] 你现在无敌，但蓝色小鸡死亡时你也会死亡！");
    }
    public void OnActivated(in SkillContext context) { }
    public void OnRevoked(in SkillContext context) => _companions.Remove(context.Player.Index);
    public void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo) { if (damageInfo.Damage > 0) damageInfo.Damage = 0; }
    public void OnTick(in SkillContext context)
    {
        if (!_companions.Update(context.Player))
        {
            context.Player.PlayerPawn.Value?.CommitSuicide(false, true);
            PluginText.ChatAll("[特殊心脏] 蓝色小鸡死亡，拥有者也随之死亡！");
        }
    }
    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event) => _companions.Remove(context.Player.Index);
}
