using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ZrySkill : ISkill, IGrenadeThrownSkill
{
    private sealed class ZryState
    {
        public bool Active { get; set; } = true;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "ZRY",
        DisplayName = "♾️ ZRY",
        Description = "拥有无限的诱饵弹，每次投掷后都会自动补充。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "decoy-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new ZryState();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => state.Active = false);
        GiveDecoy(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!string.Equals(@event.Weapon, "decoy", StringComparison.OrdinalIgnoreCase)
            || !context.State.TryGet<ZryState>(out var state)
            || !state.Active)
        {
            return;
        }

        var player = context.Player;
        var effects = context.Effects;
        effects.AddTimer(0.01f, () =>
        {
            if (state.Active)
            {
                GiveDecoy(player);
            }
        });
    }

    private static void GiveDecoy(CCSPlayerController player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var alreadyHasDecoy = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_decoy" }) == true;
        if (!alreadyHasDecoy)
        {
            player.GiveNamedItem("weapon_decoy");
        }
    }
}
