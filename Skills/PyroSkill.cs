using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class PyroSkill : ISkill, IPlayerHurtSkill, IGrenadeThrownSkill
{
    private sealed class PyroState
    {
        public required int GrenadesRemaining { get; set; }
        public bool Active { get; set; } = true;
    }

    private readonly PyroSettings _settings;

    public PyroSkill(PyroSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Pyro",
        DisplayName = "🔥 火男",
        Description = "燃烧伤害会转化为更多生命；开局获得 2 枚燃烧瓶或燃烧弹。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "inferno-damage-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new PyroState
        {
            GrenadesRemaining = Math.Clamp(_settings.GrenadeLimit, 1, 10)
        };
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => state.Active = false);
        GiveFireGrenade(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerHurt(in SkillContext context, EventPlayerHurt @event)
    {
        if (!string.Equals(@event.Weapon, "inferno", StringComparison.OrdinalIgnoreCase)
            || @event.DmgHealth <= 0
            || @event.Userid is not { IsValid: true } victim
            || victim.Slot != context.Player.Slot)
        {
            return;
        }

        var pawn = victim.PlayerPawn.Value;
        if (pawn is not { IsValid: true } || pawn.Health <= 0)
        {
            return;
        }

        var configuredMultiplier = float.IsFinite(_settings.RegenerationMultiplier)
            ? _settings.RegenerationMultiplier
            : 1.5f;
        var multiplier = Math.Clamp(configuredMultiplier, 0.0f, 10.0f);
        var healing = (int)(@event.DmgHealth * multiplier);
        if (healing <= 0)
        {
            return;
        }

        pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + healing);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if ((!GrenadeReplenishment.Matches(@event.Weapon, "molotov")
             && !GrenadeReplenishment.Matches(@event.Weapon, "incgrenade"))
            || !context.State.TryGet<PyroState>(out var state)
            || !state.Active
            || state.GrenadesRemaining <= 0)
        {
            return;
        }

        state.GrenadesRemaining--;
        if (state.GrenadesRemaining <= 0)
        {
            return;
        }

        var player = context.Player;
        context.Effects.AddTimer(GrenadeReplenishment.DelaySeconds, () =>
        {
            if (state.Active)
            {
                GiveFireGrenade(player);
            }
        });
    }

    private static void GiveFireGrenade(CCSPlayerController player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var weaponName = player.Team == CsTeam.CounterTerrorist
            ? "weapon_incgrenade"
            : "weapon_molotov";
        var alreadyHasGrenade = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true } value
                      && (value.DesignerName.Equals("weapon_molotov", StringComparison.OrdinalIgnoreCase)
                          || value.DesignerName.Equals("weapon_incgrenade", StringComparison.OrdinalIgnoreCase))) == true;
        if (!alreadyHasGrenade)
        {
            player.GiveNamedItem(weaponName);
        }
    }
}
