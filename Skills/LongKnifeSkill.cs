using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class LongKnifeSkill : ISkill, IWeaponFireSkill
{
    private readonly LongKnifeSettings _settings;
    private readonly LongRangeWeaponService _longRangeWeapons;

    public LongKnifeSkill(LongKnifeSettings settings, LongRangeWeaponService longRangeWeapons)
    {
        _settings = settings;
        _longRangeWeapons = longRangeWeapons;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "LongKnife",
        DisplayName = "🗡️ 长刀",
        Description = "刀的主攻击可以命中远处准星目标，并且一刀致命。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "enemy-damage-override",
            "knife-primary-range-control"
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

    public void OnWeaponFire(in SkillContext context, EventWeaponFire @event)
    {
        if (@event.Userid?.Slot != context.Player.Slot)
        {
            return;
        }

        var weapon = context.Player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (weapon is not { IsValid: true }
            || (weapon.DesignerName != "weapon_knife" && weapon.DesignerName != "weapon_bayonet")
            || !_longRangeWeapons.TryTracePlayer(context.Player, _settings.MaximumDistance, out var target)
            || target is null
            || (!_settings.FriendlyFire && target.Team == context.Player.Team))
        {
            return;
        }

        var damage = PositiveFiniteOr(_settings.Damage, 9999.0f);
        SkillDamage.TryDeal(context.Player, target, damage, DamageTypes_t.DMG_SLASH);
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;
}
