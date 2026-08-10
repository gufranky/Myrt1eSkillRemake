using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class EnemySpawnSkill : ISkill
{
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "EnemySpawn",
        DisplayName = "传送敌人出生点",
        Description = "按下 [css_useskill] 传送到一个随机的敌方出生点。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 15.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-teleport-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
    }

    public void OnActivated(in SkillContext context)
    {
        var player = context.Player;
        var pawn = player.PlayerPawn.Value;
        if (!player.IsValid || !player.PawnIsAlive || pawn is null || !pawn.IsValid)
        {
            return;
        }

        var spawnDesignerName = player.Team switch
        {
            CsTeam.Terrorist => "info_player_counterterrorist",
            CsTeam.CounterTerrorist => "info_player_terrorist",
            _ => string.Empty
        };
        if (spawnDesignerName.Length == 0)
        {
            return;
        }

        var spawnPoints = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(spawnDesignerName)
            .Where(spawn => spawn.IsValid && spawn.Enabled && spawn.AbsOrigin is not null)
            .ToArray();
        if (spawnPoints.Length == 0)
        {
            PluginText.Chat(player, "[传送敌人出生点] 当前地图没有可用的敌方出生点。");
            return;
        }

        var selected = spawnPoints[Random.Shared.Next(spawnPoints.Length)];
        var target = selected.AbsOrigin!;
        var position = new Vector(target.X, target.Y, target.Z);
        pawn.Teleport(position, pawn.AbsRotation, new Vector(0.0f, 0.0f, 0.0f));
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
