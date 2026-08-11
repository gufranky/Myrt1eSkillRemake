using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class LongZeusSkill : ISkill, IWeaponFireSkill
{
    private readonly LongZeusSettings _settings;
    private readonly LongRangeWeaponService _longRangeWeapons;

    public LongZeusSkill(LongZeusSettings settings, LongRangeWeaponService longRangeWeapons)
    {
        _settings = settings;
        _longRangeWeapons = longRangeWeapons;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "LongZeus",
        DisplayName = "⚡ 长宙斯",
        Description = "获得 Zeus；射击可以命中远处准星目标，并且一枪致命。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Uncommon,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "enemy-damage-override",
            "taser-range-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        if (context.Player.IsValid && context.Player.PawnIsAlive)
        {
            context.Player.GiveNamedItem("weapon_taser");
        }
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
        if (weapon is not { IsValid: true, DesignerName: "weapon_taser" }
            || !_longRangeWeapons.TryTracePlayer(context.Player, _settings.MaximumDistance, out var target)
            || target is null
            || (!_settings.FriendlyFire && target.Team == context.Player.Team))
        {
            return;
        }

        var damage = PositiveFiniteOr(_settings.Damage, 9999.0f);
        SkillDamage.TryDeal(context.Player, target, damage, DamageTypes_t.DMG_SHOCK);
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;
}
