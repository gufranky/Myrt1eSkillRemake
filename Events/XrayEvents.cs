using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class SuperpowerXrayEvent : RoundEventBase, IRoundEventPlayerDisconnect
{
    private const string GrantId = "event:SuperpowerXray";
    private readonly WallhackService _wallhack;
    private uint? _terrorist;
    private uint? _counterTerrorist;

    public SuperpowerXrayEvent(WallhackService wallhack)
    {
        _wallhack = wallhack;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "SuperpowerXray",
        DisplayName = "🦸 超能力者",
        Description = "双方各有一名玩家获得透视能力！只有超能力者能看到敌人位置！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "xray-vision-rules",
            "player-model-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-outline-vision"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _terrorist = SelectPlayer(CsTeam.Terrorist)?.Index;
        _counterTerrorist = SelectPlayer(CsTeam.CounterTerrorist)?.Index;
        UpdateGrant();
        context.Effects.RegisterCleanup(() => _wallhack.RemoveGrant(GrantId));
        PrintToChatAll("[娱乐事件] 🦸 超能力者：双方各有一名玩家获得透视能力！");
    }

    public override void OnRemoved(in RoundEventContext context)
    {
        _terrorist = null;
        _counterTerrorist = null;
    }

    public void OnPlayerDisconnect(in RoundEventContext context, EventPlayerDisconnect @event)
    {
        var disconnected = @event.Userid;
        if (disconnected is null)
        {
            return;
        }

        if (_terrorist == disconnected.Index)
        {
            _terrorist = SelectPlayer(CsTeam.Terrorist, disconnected.Index)?.Index;
        }

        if (_counterTerrorist == disconnected.Index)
        {
            _counterTerrorist = SelectPlayer(CsTeam.CounterTerrorist, disconnected.Index)?.Index;
        }

        UpdateGrant();
    }

    private CCSPlayerController? SelectPlayer(CsTeam team, uint? excludedIndex = null)
    {
        var candidates = Utilities.GetPlayers()
            .Where(player => player.IsValid
                             && player.PawnIsAlive
                             && player.Team == team
                             && player.Index != excludedIndex)
            .ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        var selected = candidates[Random.Shared.Next(candidates.Length)];
        PluginText.Chat(selected, $"[娱乐事件] 🦸 你是{(team == CsTeam.Terrorist ? "T" : "CT")}方超能力者，可以看到所有敌人！");
        return selected;
    }

    private void UpdateGrant()
    {
        var viewers = new[] { _terrorist, _counterTerrorist }
            .Where(index => index.HasValue)
            .Select(index => Utilities.GetPlayerFromIndex((int)index!.Value))
            .Where(player => player is { IsValid: true })
            .Cast<CCSPlayerController>();
        _wallhack.SetSelectiveGrant(GrantId, viewers);
    }
}

public sealed class XrayEvent : RoundEventBase
{
    private const string GrantId = "event:Xray";
    private readonly WallhackService _wallhack;

    public XrayEvent(WallhackService wallhack)
    {
        _wallhack = wallhack;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "Xray",
        DisplayName = "👁️ 全员透视",
        Description = "所有玩家可以透过墙壁看到彼此！敌我位置一览无余！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "xray-vision-rules",
            "player-model-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-outline-vision"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _wallhack.SetGlobalGrant(GrantId, includeTeammates: true);
        context.Effects.RegisterCleanup(() => _wallhack.RemoveGrant(GrantId));
        PrintToChatAll("[娱乐事件] 👁️ 全员透视：所有玩家都可以看到敌我双方位置！");
    }

    public override void OnRemoved(in RoundEventContext context)
    {
    }
}
