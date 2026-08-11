using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class GrenadierSkill : ISkill, IGrenadeThrownSkill
{
    private sealed class GrenadierState
    {
        public bool Active { get; set; } = true;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Grenadier",
        DisplayName = "💣 掷弹兵",
        Description = "你拥有无限高爆手雷。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-ammo-control",
            "hegrenade-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new GrenadierState();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => state.Active = false);
        EnsureHeGrenade(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!GrenadeReplenishment.Matches(@event.Weapon, "hegrenade")
            || !context.State.TryGet<GrenadierState>(out var state)
            || !state.Active)
        {
            return;
        }

        var player = context.Player;
        // EventGrenadeThrown fires before the thrown weapon handle is always
        // removed from MyWeapons. Waiting a few ticks avoids mistaking that stale
        // handle for a grenade the player still owns.
        context.Effects.AddTimer(GrenadeReplenishment.DelaySeconds, () =>
        {
            if (state.Active)
            {
                EnsureHeGrenade(player);
            }
        });
    }

    private static void EnsureHeGrenade(CCSPlayerController player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var alreadyHasGrenade = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_hegrenade" }) == true;
        if (!alreadyHasGrenade)
        {
            player.GiveNamedItem("weapon_hegrenade");
        }
    }
}
