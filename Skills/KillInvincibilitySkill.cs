using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class KillInvincibilitySkill : ISkill, IPlayerDeathSkill, IPreDamageSkill, ITickSkill
{
    private sealed class KillInvincibilityState
    {
        public DateTime InvincibleUntil { get; set; } = DateTime.MinValue;
        public bool Active { get; set; }
    }

    private readonly KillInvincibilitySettings _settings;

    public KillInvincibilitySkill(KillInvincibilitySettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "KillInvincibility",
        DisplayName = "⚔️ 杀戮无敌",
        Description = "击杀敌人后获得 5 秒无敌时间；连续击杀会刷新持续时间。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new KillInvincibilityState());
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        if (attacker is not { IsValid: true, PawnIsAlive: true }
            || victim is not { IsValid: true }
            || attacker.Slot != context.Player.Slot
            || attacker.Slot == victim.Slot
            || attacker.Team == victim.Team
            || !context.State.TryGet<KillInvincibilityState>(out var state))
        {
            return;
        }

        var duration = float.IsFinite(_settings.DurationSeconds)
            ? Math.Max(0.0f, _settings.DurationSeconds)
            : 5.0f;
        state.InvincibleUntil = DateTime.UtcNow.AddSeconds(duration);
        state.Active = duration > 0.0f;
        PluginText.Center(context.Player, $"⚔️ 杀戮无敌：{duration:0.#} 秒");
        PluginText.Chat(context.Player,
            $"[杀戮无敌] 击杀 {victim.PlayerName}，获得 {duration:0.#} 秒无敌！");
    }

    public void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo)
    {
        if (damageInfo.Damage <= 0.0f
            || !context.State.TryGet<KillInvincibilityState>(out var state)
            || !state.Active)
        {
            return;
        }

        if (DateTime.UtcNow >= state.InvincibleUntil)
        {
            state.Active = false;
            return;
        }

        damageInfo.Damage = 0.0f;
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<KillInvincibilityState>(out var state)
            || !state.Active
            || DateTime.UtcNow < state.InvincibleUntil)
        {
            return;
        }

        state.Active = false;
        PluginText.Chat(context.Player, "[杀戮无敌] 无敌时间结束。");
    }
}
