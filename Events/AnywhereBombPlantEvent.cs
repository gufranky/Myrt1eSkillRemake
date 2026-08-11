using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class AnywhereBombPlantEvent : RoundEventBase, IRoundEventTick, IRoundEventEntitySpawned
{
    public const int BombTimerSeconds = 60;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "AnywhereBombPlant",
        DisplayName = "💣 任意下包",
        Description = "T 方可以在地图任意有效位置下包，炸弹将在 60 秒后爆炸！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bomb-plant-rules",
            "bomb-timer-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bombsite-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        // Set this at round start so CS2's own HUD and alerts use 60 seconds.
        ConVarOverrides.Set(context.Effects, "mp_c4timer", BombTimerSeconds);
        context.Effects.RegisterCleanup(ClearForcedBombZones);
        PrintToChatAll("[娱乐事件] 💣 任意下包：T 方可在地图任意有效位置下包，爆炸倒计时为 60 秒！");
    }

    public void OnTick(in RoundEventContext context)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive || player.Team != CsTeam.Terrorist)
            {
                continue;
            }

            var pawn = player.PlayerPawn.Value;
            var activeWeapon = pawn?.WeaponServices?.ActiveWeapon.Value;
            if (pawn is not { IsValid: true }
                || activeWeapon is not { IsValid: true }
                || !activeWeapon.DesignerName.Equals("weapon_c4", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // This is the same mechanism used by jRandomSkills' Planter:
            // while C4 is held, make the native plant check see a bomb zone.
            if (!pawn.InBombZone)
            {
                pawn.InBombZone = true;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");
            }
        }
    }

    public void OnEntitySpawned(in RoundEventContext context, CEntityInstance entity)
    {
        if (!entity.DesignerName.Equals("planted_c4", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var plantedBomb = entity.As<CPlantedC4>();
        Server.NextFrame(() =>
        {
            if (plantedBomb is not { IsValid: true })
            {
                return;
            }

            // Keep the entity deadline authoritative even if another game rule
            // touched the timer between round start and the completed plant.
            plantedBomb.C4Blow = Server.CurrentTime + BombTimerSeconds;
            Utilities.SetStateChanged(plantedBomb, "CPlantedC4", "m_flC4Blow");
        });
    }

    private static void ClearForcedBombZones()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn is not { IsValid: true } || !pawn.InBombZone)
            {
                continue;
            }

            pawn.InBombZone = false;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");
        }
    }
}
