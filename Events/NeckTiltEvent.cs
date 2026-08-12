using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class NeckTiltEvent : RoundEventBase, IRoundEventPlayerSpawn
{
    private readonly NeckTiltEventSettings _settings;
    private readonly PlayerViewService _view;
    private bool _active;

    public NeckTiltEvent(NeckTiltEventSettings settings, PlayerViewService view)
    {
        _settings = settings;
        _view = view;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "NeckTilt",
        DisplayName = "歪脖子",
        Description = "所有玩家的视角都会向一侧倾斜。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "view-angle-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "view-angle-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        context.Effects.RegisterCleanup(() =>
        {
            _active = false;
            ClearRoll();
        });
        ApplyToAll();
        PrintToChatAll("[娱乐事件] 歪脖子：所有人的视角都歪过来了！");
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid;
        context.Effects.AddTimer(0.1f, () =>
        {
            if (_active && player is { IsValid: true, PawnIsAlive: true })
            {
                Apply(player);
            }
        });
    }

    private void ApplyToAll()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            Apply(player);
        }
    }

    private void Apply(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (player is not { IsValid: true, PawnIsAlive: true } || pawn is not { IsValid: true })
        {
            return;
        }

        var current = pawn.EyeAngles;
        var roll = float.IsFinite(_settings.RollDegrees)
            ? Math.Clamp(_settings.RollDegrees, -45.0f, 45.0f)
            : 25.0f;
        _view.TrySet(pawn, new QAngle(current.X, current.Y, roll));
    }

    private void ClearRoll()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player is not { IsValid: true, PawnIsAlive: true })
            {
                continue;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn is not { IsValid: true })
            {
                continue;
            }

            var current = pawn.EyeAngles;
            _view.TrySet(pawn, new QAngle(current.X, current.Y, 0.0f));
        }
    }
}
