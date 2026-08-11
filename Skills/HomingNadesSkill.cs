using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class HomingNadesSkill : ISkill, IGrenadeThrownSkill
{
    private sealed class HomingNadesState
    {
        public required int HeGrenadesRemaining { get; set; }
        public required int FlashbangsRemaining { get; set; }
        public bool Active { get; set; } = true;
    }

    private readonly HomingNadesSettings _settings;
    private readonly HomingGrenadeService _homing;

    public HomingNadesSkill(
        HomingNadesSettings settings,
        HomingGrenadeService homing)
    {
        _settings = settings;
        _homing = homing;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "HomingNades",
        DisplayName = "🧲 追踪手榴弹",
        Description = "你的手榴弹（烟雾弹除外）会被敌人吸引；获得 2 个手雷和 2 个闪光弹。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "grenade-trajectory-control",
            "hegrenade-behavior-control",
            "flashbang-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new HomingNadesState
        {
            HeGrenadesRemaining = Math.Clamp(_settings.HeGrenadeCount, 1, 10),
            FlashbangsRemaining = Math.Clamp(_settings.FlashbangCount, 1, 10)
        };
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => state.Active = false);
        _homing.Acquire(context.Player, context.Effects);
        EnsureGrenade(context.Player, "weapon_hegrenade");
        EnsureGrenade(context.Player, "weapon_flashbang");
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!context.State.TryGet<HomingNadesState>(out var state) || !state.Active)
        {
            return;
        }

        int remaining;
        if (@event.Weapon.Equals("hegrenade", StringComparison.OrdinalIgnoreCase))
        {
            remaining = --state.HeGrenadesRemaining;
        }
        else if (@event.Weapon.Equals("flashbang", StringComparison.OrdinalIgnoreCase))
        {
            remaining = --state.FlashbangsRemaining;
        }
        else
        {
            return;
        }

        if (remaining <= 0)
        {
            return;
        }

        var player = context.Player;
        var weaponName = $"weapon_{@event.Weapon}";
        context.Effects.AddTimer(0.01f, () =>
        {
            if (state.Active)
            {
                EnsureGrenade(player, weaponName);
            }
        });
    }

    private static void EnsureGrenade(CCSPlayerController player, string weaponName)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return;
        }

        var alreadyHasGrenade = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true } value
                      && value.DesignerName.Equals(weaponName, StringComparison.OrdinalIgnoreCase)) == true;
        if (!alreadyHasGrenade)
        {
            player.GiveNamedItem(weaponName);
        }
    }
}
