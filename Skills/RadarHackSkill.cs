using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class RadarHackSkill : ISkill, ITickSkill
{
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "RadarHack",
        DisplayName = "雷达黑客",
        Description = "可以在雷达上看到敌人。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "radar-vision"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var viewerSlot = context.Player.Slot;
        context.Effects.RegisterCleanup(() => ClearViewer(viewerSlot));
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        var viewer = context.Player;
        if (!viewer.IsValid || !viewer.PawnIsAlive)
        {
            return;
        }

        foreach (var enemy in Utilities.GetPlayers())
        {
            if (!enemy.IsValid
                || !enemy.PawnIsAlive
                || enemy.Team == viewer.Team
                || enemy.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
            {
                continue;
            }

            var pawn = enemy.PlayerPawn.Value;
            if (pawn is { IsValid: true })
            {
                SetViewerBit(pawn, viewer.Slot, visible: true);
            }
        }
    }

    private static void ClearViewer(int viewerSlot)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn is { IsValid: true })
            {
                SetViewerBit(pawn, viewerSlot, visible: false);
            }
        }
    }

    private static void SetViewerBit(CCSPlayerPawn pawn, int viewerSlot, bool visible)
    {
        var wordIndex = viewerSlot / 32;
        if (wordIndex < 0 || wordIndex >= pawn.EntitySpottedState.SpottedByMask.Length)
        {
            return;
        }

        var bit = 1u << (viewerSlot % 32);
        if (visible)
        {
            pawn.EntitySpottedState.SpottedByMask[wordIndex] |= bit;
        }
        else
        {
            pawn.EntitySpottedState.SpottedByMask[wordIndex] &= ~bit;
        }
    }
}
