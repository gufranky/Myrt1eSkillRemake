using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Receives CounterStrikeSharp callbacks once and dispatches them only to
/// currently assigned skills implementing the matching typed interface.
/// </summary>
public sealed class SkillEventRouter
{
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly SkillManager _skills;
    private readonly RoundEventManager _events;
    private readonly WallhackService _wallhack;
    private readonly NightmareService _nightmare;
    private readonly IlliterateService _illiterate;
    private readonly RoundPresentationService _presentation;

    public SkillEventRouter(
        Myrt1eSkillRemakePlugin plugin,
        SkillManager skills,
        RoundEventManager events,
        WallhackService wallhack,
        NightmareService nightmare,
        IlliterateService illiterate,
        RoundPresentationService presentation)
    {
        _plugin = plugin;
        _skills = skills;
        _events = events;
        _wallhack = wallhack;
        _nightmare = nightmare;
        _illiterate = illiterate;
        _presentation = presentation;
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        _events.Dispatch<IRoundEventPlayerHurt>(
            "EventRound.PlayerHurt",
            (handler, context) => handler.OnPlayerHurt(context, @event));
        _skills.Dispatch<IPlayerHurtSkill>(
            "Event.PlayerHurt",
            (handler, context) => handler.OnPlayerHurt(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        _wallhack.RemoveTarget(@event.Userid);
        _nightmare.RemoveTarget(@event.Userid);
        _skills.Dispatch<IPlayerDeathSkill>(
            "Event.PlayerDeath",
            (handler, context) => handler.OnPlayerDeath(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        _events.Dispatch<IRoundEventWeaponFire>(
            "EventRound.WeaponFire",
            (handler, context) => handler.OnWeaponFire(context, @event));
        _skills.Dispatch<IWeaponFireSkill>(
            "Event.WeaponFire",
            (handler, context) => handler.OnWeaponFire(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnBulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        _skills.DispatchForPlayer<IBulletImpactSkill>(
            @event.Userid,
            "Event.BulletImpact",
            (handler, context) => handler.OnBulletImpact(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnDecoyStarted(EventDecoyStarted @event, GameEventInfo info)
    {
        _events.Dispatch<IRoundEventDecoyStarted>(
            "EventRound.DecoyStarted",
            (handler, context) => handler.OnDecoyStarted(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        _wallhack.ScheduleTarget(@event.Userid);
        _events.Dispatch<IRoundEventPlayerSpawn>(
            "EventRound.PlayerSpawn",
            (handler, context) => handler.OnPlayerSpawn(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        _events.Dispatch<IRoundEventItemPickup>(
            "EventRound.ItemPickup",
            (handler, context) => handler.OnItemPickup(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        _events.Dispatch<IRoundEventPlayerDisconnect>(
            "EventRound.PlayerDisconnect",
            (handler, context) => handler.OnPlayerDisconnect(context, @event));
        _wallhack.RemoveTarget(@event.Userid);
        _wallhack.RemoveViewer(@event.Userid);
        _nightmare.RemoveTarget(@event.Userid);
        _nightmare.RemoveCaster(@event.Userid);
        _illiterate.RemoveHolder(@event.Userid);
        _skills.RemovePlayer(@event.Userid);
        return HookResult.Continue;
    }

    public void OnTick()
    {
        _presentation.OnTick();
        _events.Dispatch<IRoundEventTick>(
            "Tick.ActiveEvents",
            static (handler, context) => handler.OnTick(context));
        _skills.Dispatch<ITickSkill>(
            "Tick.ActiveSkills",
            static (handler, context) => handler.OnTick(context));
    }

    public void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        _wallhack.OnCheckTransmit(infoList);
        _nightmare.OnCheckTransmit(infoList);
        _events.Dispatch<IRoundEventCheckTransmit>(
            "CheckTransmit.ActiveEvents",
            (handler, context) => handler.OnCheckTransmit(context, infoList));
    }

    public void OnPlayerButtonsChanged(
        CCSPlayerController player,
        PlayerButtons pressed,
        PlayerButtons released)
    {
        if (_plugin.Config.ActivateWithUseKey && pressed.HasFlag(PlayerButtons.Use))
        {
            _skills.TryActivate(player);
        }
    }
}
