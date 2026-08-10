using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class FleetFootedSkill : ISkill
{
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "FleetFooted",
        DisplayName = "疾步",
        Description = "本回合移动速度提高 20%。",
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
        pawn.VelocityModifier *= 1.2f;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

        // The manager disposes this scope on round end, disconnect and hot reload.
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
        // Passive skill: no manual activation.
    }

    public void OnRevoked(in SkillContext context)
    {
        // Reversible state is owned by EffectScope.
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
