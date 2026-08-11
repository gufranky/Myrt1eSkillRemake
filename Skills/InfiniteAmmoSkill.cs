using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class InfiniteAmmoSkill : ISkill,
    IWeaponFireSkill,
    IWeaponReloadSkill,
    IGrenadeThrownSkill
{
    public const int RefilledClipAmmo = 100;

    private sealed class InfiniteAmmoState
    {
        public Dictionary<uint, int> OriginalClips { get; } = new();
        public bool Active { get; set; } = true;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "InfiniteAmmo",
        DisplayName = "∞ 无限弹药",
        Description = "你的所有武器都会获得无限弹药，投掷物使用后也会自动补充！",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-ammo-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new InfiniteAmmoState();
        context.State.Set(state);
        FillActiveWeapon(context.Player, state);
        context.Effects.RegisterCleanup(() => RestoreTrackedWeapons(state));
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnWeaponFire(in SkillContext context, EventWeaponFire @event)
    {
        if (context.State.TryGet<InfiniteAmmoState>(out var state) && state.Active)
        {
            FillActiveWeapon(context.Player, state);
        }
    }

    public void OnWeaponReload(in SkillContext context, EventWeaponReload @event)
    {
        if (context.State.TryGet<InfiniteAmmoState>(out var state) && state.Active)
        {
            FillActiveWeapon(context.Player, state);
        }
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!context.State.TryGet<InfiniteAmmoState>(out var state)
            || !state.Active
            || string.IsNullOrWhiteSpace(@event.Weapon))
        {
            return;
        }

        var player = context.Player;
        var weaponName = @event.Weapon.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase)
            ? @event.Weapon
            : $"weapon_{@event.Weapon}";
        context.Effects.AddTimer(0.01f, () =>
        {
            if (state.Active && player is { IsValid: true, PawnIsAlive: true })
            {
                player.GiveNamedItem(weaponName);
            }
        });
    }

    private static void FillActiveWeapon(
        CCSPlayerController player,
        InfiniteAmmoState state)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return;
        }

        var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (weapon is not { IsValid: true } || weapon.Clip1 < 0)
        {
            return;
        }

        state.OriginalClips.TryAdd(weapon.Index, weapon.Clip1);
        weapon.Clip1 = RefilledClipAmmo;
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
    }

    private static void RestoreTrackedWeapons(InfiniteAmmoState state)
    {
        state.Active = false;
        foreach (var (entityIndex, originalClip) in state.OriginalClips)
        {
            var weapon = Utilities.GetEntityFromIndex<CBasePlayerWeapon>((int)entityIndex);
            if (weapon is not { IsValid: true } || weapon.Clip1 <= originalClip)
            {
                continue;
            }

            weapon.Clip1 = originalClip;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        }

        state.OriginalClips.Clear();
    }
}
