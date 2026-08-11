using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ThornsSkill : ISkill, IPlayerHurtSkill
{
    private readonly ThornsSettings _settings;
    private bool _reflectingDamage;

    public ThornsSkill(ThornsSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Thorns",
        DisplayName = "🌹 荆棘",
        Description = "攻击你的人会受到其造成伤害的一部分。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "damage-reflection"
        }
    };

    public void OnGranted(in SkillContext context)
    {
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerHurt(in SkillContext context, EventPlayerHurt @event)
    {
        if (@event.DmgHealth <= 0
            || @event.Userid is not { IsValid: true } victim
            || victim.Slot != context.Player.Slot
            || @event.Attacker is not { IsValid: true, PawnIsAlive: true } attacker
            || attacker.Slot == victim.Slot
            || _reflectingDamage)
        {
            return;
        }

        var reflectedDamage = CalculateReflectedDamage(
            @event.DmgHealth,
            _settings.DamageScale,
            _settings.MaximumDamagePerHit);
        if (reflectedDamage <= 0)
        {
            return;
        }

        _reflectingDamage = true;
        try
        {
            if (SkillDamage.TryDeal(
                    context.Player,
                    attacker,
                    reflectedDamage,
                    DamageTypes_t.DMG_GENERIC))
            {
                var configuredVolume = float.IsFinite(_settings.SoundVolume)
                    ? _settings.SoundVolume
                    : 0.35f;
                attacker.PlayerPawn.Value?.EmitSound(
                    "Player.DamageBody.Victim",
                    volume: Math.Clamp(configuredVolume, 0.0f, 1.0f));
            }
        }
        finally
        {
            _reflectingDamage = false;
        }
    }

    public static int CalculateReflectedDamage(
        int healthDamage,
        float damageScale,
        int maximumDamagePerHit)
    {
        var scale = float.IsFinite(damageScale)
            ? Math.Clamp(damageScale, 0.0f, 10.0f)
            : 0.30f;
        return Math.Min(
            Math.Max(0, maximumDamagePerHit),
            (int)(Math.Max(0, healthDamage) * scale));
    }
}
