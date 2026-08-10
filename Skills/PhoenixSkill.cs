using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class PhoenixSkill : ISkill, IPreDamageSkill
{
    private sealed class PhoenixState
    {
        public required float ReviveChance { get; init; }
        public int ProtectedUntilTick { get; set; } = -1;
    }

    private readonly PhoenixSettings _settings;
    private readonly ReviveService _revives;

    public PhoenixSkill(PhoenixSettings settings, ReviveService revives)
    {
        _settings = settings;
        _revives = revives;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Phoenix",
        DisplayName = "🔥 凤凰",
        Description = "受到致命伤害时有 20%～40% 的随机概率涅槃重生。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "second-chance"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var configuredMinimum = float.IsFinite(_settings.MinimumChance) ? _settings.MinimumChance : 0.20f;
        var configuredMaximum = float.IsFinite(_settings.MaximumChance) ? _settings.MaximumChance : 0.40f;
        var minimum = Math.Clamp(Math.Min(configuredMinimum, configuredMaximum), 0.0f, 1.0f);
        var maximum = Math.Clamp(Math.Max(configuredMinimum, configuredMaximum), minimum, 1.0f);
        var chance = minimum + Random.Shared.NextSingle() * (maximum - minimum);
        context.State.Set(new PhoenixState { ReviveChance = chance });
        PluginText.Chat(context.Player, $"[凤凰] 本回合涅槃概率为 {chance:P0}。");
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo)
    {
        if (!context.State.TryGet<PhoenixState>(out var state))
        {
            return;
        }

        if (Server.TickCount <= state.ProtectedUntilTick)
        {
            damageInfo.Damage = 0.0f;
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is not { IsValid: true }
            || !DamageEvaluation.WouldBeLethal(pawn, damageInfo)
            || Random.Shared.NextSingle() > state.ReviveChance)
        {
            return;
        }

        var health = Math.Max(1, _settings.ReviveHealth);
        if (!_revives.TryRevive(context.Player, health))
        {
            return;
        }

        state.ProtectedUntilTick = Server.TickCount + 4;
        damageInfo.Damage = 0.0f;
        PluginText.Center(context.Player, "🔥 凤凰涅槃！");
        PluginText.ChatAll($"🔥 {context.Player.PlayerName} 从灰烬中重生了！");
    }
}
