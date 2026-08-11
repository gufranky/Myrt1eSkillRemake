using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class NinjaSkill : ISkill, ITickSkill
{
    private readonly NinjaSettings _settings;
    private readonly NinjaVisibilityService _visibility;

    public NinjaSkill(NinjaSettings settings, NinjaVisibilityService visibility)
    {
        _settings = settings;
        _visibility = visibility;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Ninja",
        DisplayName = "🥷 忍者",
        Description = "静止、蹲下、持刀分别提升 33% 隐身，三个效果可以叠加。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-visibility-control",
            "player-model-control",
            "player-render-color-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        if (!_visibility.Acquire(context.Player, context.Effects))
        {
            throw new InvalidOperationException("Ninja could not acquire player visibility control.");
        }

        Update(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        if (Server.TickCount % 2 == 0)
        {
            Update(context.Player);
        }
    }

    private void Update(CCSPlayerController player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            _visibility.SetInvisibility(player, 0.0f);
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        var flags = (PlayerFlags)pawn.Flags;
        var idle = flags.HasFlag(PlayerFlags.FL_ONGROUND)
                   && !player.Buttons.HasFlag(PlayerButtons.Moveleft)
                   && !player.Buttons.HasFlag(PlayerButtons.Moveright)
                   && !player.Buttons.HasFlag(PlayerButtons.Forward)
                   && !player.Buttons.HasFlag(PlayerButtons.Back);
        var crouching = player.Buttons.HasFlag(PlayerButtons.Duck);
        var weaponName = pawn.WeaponServices?.ActiveWeapon.Value?.DesignerName;
        var holdingKnife = IsKnife(weaponName);

        _visibility.SetInvisibility(
            player,
            CalculateInvisibility(idle, crouching, holdingKnife, _settings));
    }

    public static float CalculateInvisibility(
        bool idle,
        bool crouching,
        bool holdingKnife,
        NinjaSettings settings)
    {
        var invisibility = 0.0f;
        if (idle)
        {
            invisibility += SafeContribution(settings.IdleInvisibility);
        }

        if (crouching)
        {
            invisibility += SafeContribution(settings.CrouchInvisibility);
        }

        if (holdingKnife)
        {
            invisibility += SafeContribution(settings.KnifeInvisibility);
        }

        return Math.Clamp(invisibility, 0.0f, 1.0f);
    }

    public static bool IsKnife(string? designerName) =>
        !string.IsNullOrWhiteSpace(designerName)
        && (designerName.Contains("knife", StringComparison.OrdinalIgnoreCase)
            || designerName.Contains("bayonet", StringComparison.OrdinalIgnoreCase));

    private static float SafeContribution(float value) =>
        float.IsFinite(value) ? Math.Clamp(value, 0.0f, 1.0f) : 0.33f;
}
