using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class StandingStillBombsEvent : RoundEventBase
{
    private readonly StandingStillBombsEventSettings _settings;
    private readonly ExplosiveProjectileService _explosions;
    private bool _active;

    public StandingStillBombsEvent(StandingStillBombsEventSettings settings, ExplosiveProjectileService explosions)
    {
        _settings = settings;
        _explosions = explosions;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "StandingStillBombs",
        DisplayName = "💣 不动就炸",
        Description = "每隔 5 秒在每位存活玩家脚下生成一颗手雷，两秒后爆炸；尽快离开原位！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "explosion-rules"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        context.Effects.RegisterCleanup(() => _active = false);
        var interval = PositiveOr(_settings.IntervalSeconds, 5.0f);
        context.Effects.AddTimer(interval, SpawnBombs, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        PrintToChatAll("[娱乐事件] 💣 不动就炸：每 5 秒脚下会出现一颗 2 秒后爆炸的手雷！");
    }

    private void SpawnBombs()
    {
        if (!_active)
        {
            return;
        }

        var damage = NonNegativeOr(_settings.Damage, 100.0f);
        var radius = PositiveOr(_settings.Radius, 250.0f);
        var fuse = NonNegativeOr(_settings.FuseSeconds, 2.0f);
        foreach (var player in Utilities.GetPlayers())
        {
            var origin = player.PlayerPawn.Value?.AbsOrigin;
            if (player is not { IsValid: true, PawnIsAlive: true } || origin is null)
            {
                continue;
            }

            _explosions.TrySpawnDelayed(
                new Vector(origin.X, origin.Y, origin.Z + 4.0f),
                player,
                damage,
                radius,
                fuse);
        }
    }

    private static float PositiveOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static float NonNegativeOr(float value, float fallback) =>
        float.IsFinite(value) && value >= 0.0f ? value : fallback;
}
