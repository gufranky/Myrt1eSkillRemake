using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class FriendlyFireSkill : ISkill, IPreDamageAttackerSkill
{
    private readonly FriendlyFireSettings _settings;
    private readonly FriendlyFireService _friendlyFire;

    public FriendlyFireSkill(FriendlyFireSettings settings, FriendlyFireService friendlyFire)
    {
        _settings = settings;
        _friendlyFire = friendlyFire;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "FriendlyFire",
        DisplayName = "友军误伤",
        Description = "射击队友不会造成伤害，反而会治疗他们。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        RequiresTeammate = true,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "friendly-fire-damage-transform"
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

    public void OnBeforeDamageDealt(
        in SkillContext context,
        CCSPlayerController victim,
        CTakeDamageInfo damageInfo)
    {
        if (!victim.IsValid
            || !victim.PawnIsAlive
            || victim.Team != context.Player.Team
            || damageInfo.AmmoType == 255
            || damageInfo.Damage <= 0.0f)
        {
            return;
        }

        var pawn = victim.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid)
        {
            return;
        }

        var rawDamage = damageInfo.Damage;
        damageInfo.Damage = 0.0f;
        _friendlyFire.SuppressAutoKick();

        var configuredMultiplier = float.IsFinite(_settings.HealthDamageMultiplier)
            ? _settings.HealthDamageMultiplier
            : 0.30f;
        var multiplier = Math.Clamp(configuredMultiplier, 0.0f, 10.0f);
        var healing = (int)(rawDamage * multiplier);
        if (healing <= 0)
        {
            return;
        }

        pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + healing);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }
}
