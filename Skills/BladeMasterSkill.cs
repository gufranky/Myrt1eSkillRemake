using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class BladeMasterSkill : ISkill, IPreDamageSkill
{
    private readonly BladeMasterSettings _settings;

    public BladeMasterSkill(BladeMasterSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "BladeMaster",
        DisplayName = "⚔️ 刀锋大师",
        Description = "持刀时，你有很高概率能偏转子弹！",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "conditional-bullet-immunity"
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

    public void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo)
    {
        if (damageInfo.Damage <= 0.0f
            || !damageInfo.BitsDamageType.HasFlag(DamageTypes_t.DMG_BULLET)
            || !IsHoldingKnife(context.Player))
        {
            return;
        }

        var attackerPawn = damageInfo.Attacker?.Value?.As<CCSPlayerPawn>();
        var attacker = attackerPawn?.Controller.Value?.As<CCSPlayerController>();
        if (attacker is null || !attacker.IsValid || attacker.Slot == context.Player.Slot)
        {
            return;
        }

        var chance = GetDeflectionChance(damageInfo.GetHitGroup(), _settings);
        if (Random.Shared.NextSingle() > chance)
        {
            return;
        }

        damageInfo.Damage = 0.0f;
        damageInfo.TotalledDamage = 0.0f;
        damageInfo.StoppedBullet = true;
        damageInfo.ShouldBleed = false;
        damageInfo.ShouldSpark = true;
        PluginText.Center(context.Player, "⚔️ 子弹偏转！");
    }

    public static float GetDeflectionChance(HitGroup_t hitGroup, BladeMasterSettings settings)
    {
        var configured = hitGroup is HitGroup_t.HITGROUP_LEFTLEG or HitGroup_t.HITGROUP_RIGHTLEG
            ? settings.LegDeflectionChance
            : settings.TorsoDeflectionChance;
        return float.IsFinite(configured) ? Math.Clamp(configured, 0.0f, 1.0f) : 0.0f;
    }

    public static bool IsKnifeDesignerName(string? designerName) =>
        !string.IsNullOrWhiteSpace(designerName)
        && (designerName.Contains("knife", StringComparison.OrdinalIgnoreCase)
            || designerName.Contains("bayonet", StringComparison.OrdinalIgnoreCase));

    private static bool IsHoldingKnife(CCSPlayerController player)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return false;
        }

        var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        return weapon is { IsValid: true } && IsKnifeDesignerName(weapon.DesignerName);
    }
}
