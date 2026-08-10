using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class MeitoSkill : ISkill, IPreDamageSkill
{
    private const float InvincibilitySeconds = 0.75f;

    private sealed class MeitoState
    {
        public bool Used { get; set; }
        public DateTime InvincibleUntil { get; set; } = DateTime.MinValue;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Meito",
        DisplayName = "⚔️ 名刀",
        Description = "抵消一次致命伤害并获得 0.75 秒无敌，每回合限用一次。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Rare,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "second-chance"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new MeitoState());
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo)
    {
        if (damageInfo.Damage <= 0.0f || !context.State.TryGet<MeitoState>(out var state))
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (now < state.InvincibleUntil)
        {
            damageInfo.Damage = 0.0f;
            return;
        }

        if (state.Used)
        {
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid || damageInfo.Damage < pawn.Health)
        {
            return;
        }

        state.Used = true;
        state.InvincibleUntil = now.AddSeconds(InvincibilitySeconds);
        damageInfo.Damage = 0.0f;

        PluginText.Center(context.Player, "⚔️ 名刀御守！");
        PluginText.Chat(context.Player, "[名刀] 已抵消致命伤害，并获得 0.75 秒无敌！");
        PluginText.ChatAll($"⚔️ {context.Player.PlayerName} 使用了名刀！");
    }
}
