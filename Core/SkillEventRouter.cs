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
    private readonly DarknessService _darkness;
    private readonly IlliterateService _illiterate;
    private readonly GhostService _ghosts;
    private readonly ChickenService _chickens;
    private readonly GlazService _glaz;
    private readonly RoundPresentationService _presentation;
    private readonly CrosshairSuppressionService _crosshairs;
    private readonly TrackerTrailService _tracker;
    private readonly NinjaVisibilityService _ninjaVisibility;

    public SkillEventRouter(
        Myrt1eSkillRemakePlugin plugin,
        SkillManager skills,
        RoundEventManager events,
        WallhackService wallhack,
        NightmareService nightmare,
        DarknessService darkness,
        IlliterateService illiterate,
        GhostService ghosts,
        ChickenService chickens,
        GlazService glaz,
        RoundPresentationService presentation,
        CrosshairSuppressionService crosshairs,
        TrackerTrailService tracker,
        NinjaVisibilityService ninjaVisibility)
    {
        _plugin = plugin;
        _skills = skills;
        _events = events;
        _wallhack = wallhack;
        _nightmare = nightmare;
        _darkness = darkness;
        _illiterate = illiterate;
        _ghosts = ghosts;
        _chickens = chickens;
        _glaz = glaz;
        _presentation = presentation;
        _crosshairs = crosshairs;
        _tracker = tracker;
        _ninjaVisibility = ninjaVisibility;
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

    public HookResult OnPlayerHurtPre(EventPlayerHurt @event, GameEventInfo info)
    {
        if (@event.DmgHealth <= 0 && @event.DmgArmor <= 0)
        {
            return HookResult.Continue;
        }

        _skills.DispatchForPlayer<IPlayerHurtPreSkill>(
            @event.Userid,
            "Event.PlayerHurt.Pre",
            (handler, context) => handler.OnPlayerHurtPre(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        _wallhack.RemoveTarget(@event.Userid);
        _nightmare.RemoveTarget(@event.Userid);
        _darkness.RemoveTarget(@event.Userid);
        _skills.Dispatch<IPlayerDeathSkill>(
            "Event.PlayerDeath",
            (handler, context) => handler.OnPlayerDeath(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
    {
        _skills.Dispatch<IPlayerBlindSkill>(
            "Event.PlayerBlind",
            (handler, context) => handler.OnPlayerBlind(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnFlashbangDetonate(EventFlashbangDetonate @event, GameEventInfo info)
    {
        _skills.DispatchForPlayer<IFlashbangDetonateSkill>(
            @event.Userid,
            "Event.FlashbangDetonate",
            (handler, context) => handler.OnFlashbangDetonate(context, @event));
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

    public HookResult OnPlayerJump(EventPlayerJump @event, GameEventInfo info)
    {
        _skills.DispatchForPlayer<IPlayerJumpSkill>(
            @event.Userid,
            "Event.PlayerJump",
            (handler, context) => handler.OnPlayerJump(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnWeaponReload(EventWeaponReload @event, GameEventInfo info)
    {
        _skills.DispatchForPlayer<IWeaponReloadSkill>(
            @event.Userid,
            "Event.WeaponReload",
            (handler, context) => handler.OnWeaponReload(context, @event));
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
        _skills.DispatchForPlayer<IDecoyStartedSkill>(
            @event.Userid,
            "Event.DecoyStarted",
            (handler, context) => handler.OnDecoyStarted(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnDecoyDetonate(EventDecoyDetonate @event, GameEventInfo info)
    {
        _skills.DispatchForPlayer<IDecoyDetonateSkill>(
            @event.Userid,
            "Event.DecoyDetonate",
            (handler, context) => handler.OnDecoyDetonate(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
    {
        _events.Dispatch<IRoundEventGrenadeThrown>(
            "EventRound.GrenadeThrown",
            (handler, context) => handler.OnGrenadeThrown(context, @event));
        _skills.DispatchForPlayer<IGrenadeThrownSkill>(
            @event.Userid,
            "Event.GrenadeThrown",
            (handler, context) => handler.OnGrenadeThrown(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnSmokeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
    {
        _glaz.OnSmokeDetonate(@event);
        _skills.DispatchForPlayer<ISmokeDetonateSkill>(
            @event.Userid,
            "Event.SmokeDetonate",
            (handler, context) => handler.OnSmokeDetonate(context, @event));
        return HookResult.Continue;
    }

    public HookResult OnSmokeExpired(EventSmokegrenadeExpired @event, GameEventInfo info)
    {
        _glaz.OnSmokeExpired(@event);
        _skills.DispatchForPlayer<ISmokeExpiredSkill>(
            @event.Userid,
            "Event.SmokeExpired",
            (handler, context) => handler.OnSmokeExpired(context, @event));
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
        _plugin.WasdMenus.Close(@event.Userid);
        _events.Dispatch<IRoundEventPlayerDisconnect>(
            "EventRound.PlayerDisconnect",
            (handler, context) => handler.OnPlayerDisconnect(context, @event));
        _wallhack.RemoveTarget(@event.Userid);
        _wallhack.RemoveViewer(@event.Userid);
        _nightmare.RemoveTarget(@event.Userid);
        _nightmare.RemoveCaster(@event.Userid);
        _darkness.RemoveTarget(@event.Userid);
        _darkness.RemoveCaster(@event.Userid, notifyTarget: false);
        _illiterate.RemoveHolder(@event.Userid);
        _crosshairs.ClearPlayer(@event.Userid);
        _skills.RemovePlayer(@event.Userid);
        return HookResult.Continue;
    }

    public void OnTick()
    {
        _presentation.OnTick();
        _plugin.WasdMenus.OnTick();
        _events.Dispatch<IRoundEventTick>(
            "Tick.ActiveEvents",
            static (handler, context) => handler.OnTick(context));
        _skills.Dispatch<ITickSkill>(
            "Tick.ActiveSkills",
            static (handler, context) => handler.OnTick(context));
    }

    public void OnEntitySpawned(CEntityInstance entity)
    {
        _events.Dispatch<IRoundEventEntitySpawned>(
            "EntitySpawned.ActiveEvents",
            (handler, context) => handler.OnEntitySpawned(context, entity));

        if (!string.Equals(entity.DesignerName, "smokegrenade_projectile", StringComparison.Ordinal))
        {
            return;
        }

        Server.NextFrame(() =>
        {
            if (!entity.IsValid)
            {
                return;
            }

            var grenade = entity.As<CBaseCSGrenadeProjectile>();
            var pawn = grenade?.OwnerEntity.Value?.As<CCSPlayerPawn>();
            var owner = pawn?.Controller.Value?.As<CCSPlayerController>();
            _skills.DispatchForPlayer<IEntitySpawnedSkill>(
                owner,
                "EntitySpawned.PlayerSkills",
                (handler, context) => handler.OnEntitySpawned(context, entity));
        });
    }

    public void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        _wallhack.OnCheckTransmit(infoList);
        _ghosts.OnCheckTransmit(infoList);
        _chickens.OnCheckTransmit(infoList);
        _glaz.OnCheckTransmit(infoList);
        _nightmare.OnCheckTransmit(infoList);
        _tracker.OnCheckTransmit(infoList);
        _ninjaVisibility.OnCheckTransmit(infoList);
        _events.Dispatch<IRoundEventCheckTransmit>(
            "CheckTransmit.ActiveEvents",
            (handler, context) => handler.OnCheckTransmit(context, infoList));
    }

    public void OnPlayerButtonsChanged(
        CCSPlayerController player,
        PlayerButtons pressed,
        PlayerButtons released)
    {
        if (_plugin.WasdMenus.HandleButtons(player, pressed))
        {
            return;
        }

        _skills.DispatchForPlayer<IPlayerButtonsChangedSkill>(
            player,
            "Input.PlayerButtonsChanged",
            (handler, context) => handler.OnPlayerButtonsChanged(context, pressed, released));

        if (_plugin.Config.ActivateWithUseKey && pressed.HasFlag(PlayerButtons.Use))
        {
            var activeSkills = _skills.GetActiveSkills(player);
            if (activeSkills.Count <= 1)
            {
                _skills.TryActivate(player);
                return;
            }

            var menu = new WasdMenu(PluginText.Transform(player, "选择要发动的技能"), _plugin);
            foreach (var skill in activeSkills)
            {
                var skillId = skill.Id;
                menu.AddMenuOption(
                    PluginText.Transform(player, $"{skill.DisplayName}：{skill.Description}"),
                    (menuPlayer, option) => _skills.TryActivate(menuPlayer, skillId));
            }

            _plugin.WasdMenus.Open(player, menu);
        }
    }
}
