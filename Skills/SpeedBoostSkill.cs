using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class SpeedBoostSkill : ISkill
{
    private const float SpeedMultiplier = 1.5f;

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "SpeedBoost",
        DisplayName = "⚡ 速度提升",
        Description = "移动速度提升50%！",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "movement-speed"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var pawn = GetPawn(context.Player);
        if (pawn is null)
        {
            return;
        }

        var originalModifier = pawn.VelocityModifier;
        var player = context.Player;
        pawn.VelocityModifier = originalModifier * SpeedMultiplier;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

        context.Effects.RegisterCleanup(() =>
        {
            var currentPawn = GetPawn(player);
            if (currentPawn is null)
            {
                return;
            }

            currentPawn.VelocityModifier = originalModifier;
            Utilities.SetStateChanged(currentPawn, "CCSPlayerPawn", "m_flVelocityModifier");
        });
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    private static CCSPlayerPawn? GetPawn(CCSPlayerController player)
    {
        if (!player.IsValid)
        {
            return null;
        }

        var pawn = player.PlayerPawn.Value;
        return pawn is { IsValid: true } ? pawn : null;
    }
}
