using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class BombardmentZoneEvent : RoundEventBase
{
    private readonly ExplosiveProjectileService _explosions;
    private readonly NavMeshService _navMesh;
    private readonly BombardmentZoneSettings _settings;
    public BombardmentZoneEvent(BombardmentZoneSettings settings, ExplosiveProjectileService explosions, NavMeshService navMesh)
    { _settings = settings; _explosions = explosions; _navMesh = navMesh; }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "BombardmentZone", DisplayName = "轰炸区", Description = "每隔 1 秒，地图随机位置发生一次爆炸，造成 20 点伤害。", DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "explosion-rules" }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        context.Effects.AddTimer(1.0f, () => Strike(), TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        PrintToChatAll("[娱乐事件] 轰炸区：每秒都会有随机位置遭到轰炸！");
    }

    private void Strike()
    {
        var owner = Utilities.GetPlayers().FirstOrDefault(player => player is { IsValid: true, PawnIsAlive: true });
        if (owner is null || !_navMesh.TryFindSafeRandomPosition(owner, out var position, out _)) return;
        var damage = float.IsFinite(_settings.Damage) ? Math.Max(0.0f, _settings.Damage) : 20.0f;
        var radius = float.IsFinite(_settings.Radius) ? Math.Max(1.0f, _settings.Radius) : 180.0f;
        _explosions.TrySpawn(position, owner, damage, radius);
    }
}
