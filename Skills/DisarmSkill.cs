using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class DisarmSkill : ISkill, IPlayerHurtSkill
{
    private const float DefaultMinimumChance = 0.20f;
    private const float DefaultMaximumChance = 0.35f;
    private sealed record DisarmState(float Chance);

    private readonly DisarmSettings _settings;

    public DisarmSkill(DisarmSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Disarm",
        DisplayName = "✂️ 裁军",
        Description = "击中敌人时有 20%～35% 的随机概率使其掉落当前武器。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "on-hit-weapon-drop"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var configuredMinimum = float.IsFinite(_settings.MinimumChance)
            ? _settings.MinimumChance
            : DefaultMinimumChance;
        var configuredMaximum = float.IsFinite(_settings.MaximumChance)
            ? _settings.MaximumChance
            : DefaultMaximumChance;
        var minimum = Math.Clamp(Math.Min(configuredMinimum, configuredMaximum), 0.0f, 1.0f);
        var maximum = Math.Clamp(Math.Max(configuredMinimum, configuredMaximum), minimum, 1.0f);
        var chance = minimum + Random.Shared.NextSingle() * (maximum - minimum);

        context.State.Set(new DisarmState(chance));
        PluginText.Chat(context.Player, $"[裁军] 本回合触发概率为 {chance:P0}。");
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerHurt(in SkillContext context, EventPlayerHurt @event)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        if (attacker is null
            || victim is null
            || !attacker.IsValid
            || !victim.IsValid
            || attacker.Slot != context.Player.Slot
            || attacker.Slot == victim.Slot
            || attacker.Team == victim.Team
            || !victim.PawnIsAlive
            || !context.State.TryGet<DisarmState>(out var state)
            || Random.Shared.NextSingle() > state.Chance)
        {
            return;
        }

        var weapon = victim.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (weapon is not { IsValid: true })
        {
            return;
        }

        var weaponName = weapon.DesignerName;
        if (weaponName.Contains("weapon_knife", StringComparison.OrdinalIgnoreCase)
            || weaponName.Contains("weapon_bayonet", StringComparison.OrdinalIgnoreCase)
            || weaponName.Contains("weapon_c4", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        victim.DropActiveWeapon();
        PluginText.Chat(attacker, $"[裁军] 你使 {victim.PlayerName} 掉落了武器！");
        PluginText.Chat(victim, $"[裁军] 你的武器被 {attacker.PlayerName} 打落了！");
    }
}
