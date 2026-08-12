using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class ManyChickensEvent : RoundEventBase, IRoundEventTick, IRoundEventPlayerSpawn
{
    private const string ChickenDesignerName = "chicken";
    private readonly ManyChickensEventSettings _settings;
    private readonly ChickenSettings _chickenSettings;
    private readonly NavMeshService _navMesh;
    private readonly ChickenService _chickens;
    private readonly List<CChicken> _ambientChickens = new();
    private bool _active;

    public ManyChickensEvent(
        ManyChickensEventSettings settings,
        ChickenSettings chickenSettings,
        NavMeshService navMesh,
        ChickenService chickens)
    {
        _settings = settings;
        _chickenSettings = chickenSettings;
        _navMesh = navMesh;
        _chickens = chickens;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "ManyChickens",
        DisplayName = "好多小鸡",
        Description = "所有玩家变成小鸡，随机出生，并在地图上生成 50 只小鸡；雷达保持开启。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-model-rules", "radar-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-model-control",
            "player-scale-control",
            "radar-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        ConVarOverrides.Set(context.Effects, "sv_disable_radar", false);
        context.Effects.RegisterCleanup(() =>
        {
            _active = false;
            foreach (var chicken in _ambientChickens)
            {
                if (chicken.IsValid)
                {
                    chicken.Remove();
                }
            }

            _ambientChickens.Clear();
        });

        foreach (var player in Utilities.GetPlayers().Where(player => player is { IsValid: true, PawnIsAlive: true }))
        {
            _chickens.Apply(player);
            RandomizeSpawn(player);
        }

        SpawnAmbientChickens(context.Effects);
        PrintToChatAll("[娱乐事件] 好多小鸡：所有人都变成小鸡了！地图上还有 50 只小鸡，雷达保持开启！");
    }

    public void OnTick(in RoundEventContext context)
    {
        if (!_active)
        {
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            _chickens.Update(player);
        }
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid;
        var effects = context.Effects;
        effects.AddTimer(0.2f, () =>
        {
            if (!_active || player is not { IsValid: true, PawnIsAlive: true })
            {
                return;
            }

            _chickens.Apply(player);
            RandomizeSpawn(player);
        });
    }

    private void SpawnAmbientChickens(EffectScope effects)
    {
        var probe = Utilities.GetPlayers().FirstOrDefault(player => player is { IsValid: true, PawnIsAlive: true });
        if (probe is null)
        {
            return;
        }

        var count = Math.Clamp(_settings.ChickenCount, 1, 200);
        for (var i = 0; i < count; i++)
        {
            if (!_navMesh.TryFindSafeRandomPosition(probe, out var position, out _))
            {
                break;
            }

            var chicken = Utilities.CreateEntityByName<CChicken>(ChickenDesignerName);
            if (chicken is null)
            {
                continue;
            }

            chicken.DispatchSpawn();
            chicken.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            chicken.MaxHealth = 30;
            chicken.Health = 30;
            Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iMaxHealth");
            _ambientChickens.Add(chicken);
            effects.TrackEntity(chicken);
        }
    }

    private void RandomizeSpawn(CCSPlayerController player)
    {
        if (!_navMesh.TryFindSafeRandomPosition(player, out var destination, out _))
        {
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is { IsValid: true })
        {
            pawn.Teleport(destination, pawn.AbsRotation, new Vector(0, 0, 0));
        }
    }
}
