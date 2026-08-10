using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Myrt1eSkill_Remake.Core;

public sealed class ReviveService
{
    public bool TryRevive(CCSPlayerController player, int health)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return false;
        }

        var spawnName = player.Team switch
        {
            CsTeam.Terrorist => "info_player_terrorist",
            CsTeam.CounterTerrorist => "info_player_counterterrorist",
            _ => string.Empty
        };
        if (spawnName.Length == 0)
        {
            return false;
        }

        var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(spawnName)
            .Where(spawn => spawn.IsValid && spawn.Enabled && spawn.AbsOrigin is not null)
            .ToArray();
        if (spawns.Length == 0)
        {
            return false;
        }

        var selected = spawns[Random.Shared.Next(spawns.Length)];
        var origin = selected.AbsOrigin!;
        var destination = new Vector(origin.X, origin.Y, origin.Z);
        var rotation = selected.AbsRotation is null
            ? pawn.AbsRotation
            : new QAngle(selected.AbsRotation.X, selected.AbsRotation.Y, selected.AbsRotation.Z);

        pawn.Health = Math.Clamp(health, 1, Math.Max(1, pawn.MaxHealth));
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Server.NextFrame(() =>
        {
            if (player.IsValid && player.PawnIsAlive && pawn.IsValid)
            {
                pawn.Teleport(destination, rotation, new Vector(0.0f, 0.0f, 0.0f));
            }
        });
        return true;
    }
}
