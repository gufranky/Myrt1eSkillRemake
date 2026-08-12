using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake;

[MinimumApiVersion(371)]
public sealed class Myrt1eSkillRemakePlugin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Myrt1eSkill_Remake";
    public override string ModuleVersion => "0.51.0-dev";
    public override string ModuleAuthor => "gufranky and contributors";
    public override string ModuleDescription => "A modular random-skill entertainment plugin for CS2.";

    public PluginConfig Config { get; set; } = new();

    private SkillRegistry _registry = null!;
    private SkillManager _skillManager = null!;
    private RoundCoordinator _roundCoordinator = null!;
    private SkillEventRouter _eventRouter = null!;
    private EventRegistry _eventRegistry = null!;
    private RoundEventManager _roundEventManager = null!;
    private DamageEventRouter _damageEventRouter = null!;
    private ExplosiveProjectileService _explosions = null!;
    private ExplodingBarrelService _barrels = null!;
    private FortniteService _fortnite = null!;
    private IllusionistService _illusionist = null!;
    private LongRangeWeaponService _longRangeWeapons = null!;
    private GrappleService _grapple = null!;
    private ThrowingKnifeService _throwingKnives = null!;
    private FireRainService _fireRain = null!;
    private FriendlyFireService _friendlyFire = null!;
    private PlayerViewService _playerView = null!;
    private NoRecoilService _noRecoil = null!;
    private BombMinerService _bombMiner = null!;
    private WallhackService _wallhack = null!;
    private NightmareService _nightmare = null!;
    private DarknessService _darkness = null!;
    private HomingGrenadeService _homingGrenades = null!;
    private SpectatorCameraService _spectator = null!;
    private IlliterateService _illiterate = null!;
    private ThirdEyeService _thirdEye = null!;
    private FalconEyeService _falconEye = null!;
    private CypherCameraService _cypher = null!;
    private ReviveService _revives = null!;
    private GhostService _ghosts = null!;
    private ChickenService _chickens = null!;
    private HealingChickenService _healingChickens = null!;
    private SpecialHeartCompanionService _specialHeart = null!;
    private HelpingHandService _helpingHand = null!;
    private FindThemService _findThem = null!;
    private KamikazeChickenService _kamikazeChickens = null!;
    private GlazService _glaz = null!;
    private HolyHandGrenadeService _holyHandGrenades = null!;
    private RoundPresentationService _presentation = null!;
    private CrosshairSuppressionService _crosshairs = null!;
    private DeafSoundService _deafSounds = null!;
    private FieldOfViewService _fieldOfView = null!;
    private TrackerTrailService _tracker = null!;
    private SilentSoundService _silentSounds = null!;
    private MindHackService _mindHack = null!;
    private NavMeshService _navMesh = null!;
    private FogService _fog = null!;
    private NinjaVisibilityService _ninjaVisibility = null!;
    private WasdMenuService _wasdMenus = null!;

    internal SkillManager RuntimeSkills => _skillManager;
    internal RoundPresentationService RuntimePresentation => _presentation;
    internal WasdMenuService WasdMenus => _wasdMenus;

    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        _wasdMenus = new WasdMenuService(this);
        _navMesh = new NavMeshService(this);
        _navMesh.Load();
        _fog = new FogService();
        _explosions = new ExplosiveProjectileService(this, Config.ExplosiveShot);
        _explosions.Load();
        _barrels = new ExplodingBarrelService(this, Config.ExplodingBarrel, _explosions);
        _barrels.Load();
        _fortnite = new FortniteService(this, Config.Fortnite);
        _fortnite.Load();
        _illusionist = new IllusionistService(this, Config.Illusionist);
        _illusionist.Load();
        _longRangeWeapons = new LongRangeWeaponService(this);
        _grapple = new GrappleService(this, Config.Grapple);
        _grapple.Load();
        _throwingKnives = new ThrowingKnifeService(this);
        _throwingKnives.Load();
        _fireRain = new FireRainService(this);
        _fireRain.Load();
        _friendlyFire = new FriendlyFireService();
        _playerView = new PlayerViewService(this);
        _noRecoil = new NoRecoilService();
        _bombMiner = new BombMinerService(this, Config.BombMiner);
        _bombMiner.Load();
        _wallhack = new WallhackService(this);
        _nightmare = new NightmareService(this, Config.Nightmare);
        _darkness = new DarknessService(this, Config.Darkness);
        _homingGrenades = new HomingGrenadeService(this, Config.HomingNades);
        _homingGrenades.Load();
        _spectator = new SpectatorCameraService(this, Config.Spectator);
        _spectator.Load();
        _illiterate = new IlliterateService();
        _thirdEye = new ThirdEyeService(this, Config.ThirdEye);
        _thirdEye.Load();
        _falconEye = new FalconEyeService(this, Config.FalconEye);
        _falconEye.Load();
        _cypher = new CypherCameraService(this, Config.Cypher, _playerView);
        _cypher.Load();
        _revives = new ReviveService();
        _ghosts = new GhostService();
        _chickens = new ChickenService(this, Config.Chicken);
        _chickens.Load();
        _healingChickens = new HealingChickenService(this, Config.HealingChicken);
        _healingChickens.Load();
        _specialHeart = new SpecialHeartCompanionService(this, Config.SpecialHeart);
        _specialHeart.Load();
        _helpingHand = new HelpingHandService(Config.HelpingHand);
        _findThem = new FindThemService(Config.FindThem);
        _kamikazeChickens = new KamikazeChickenService(Config.KamikazeChicken, _explosions);
        _glaz = new GlazService();
        _holyHandGrenades = new HolyHandGrenadeService(this, Config.HolyHandGrenade);
        _holyHandGrenades.Load();
        _crosshairs = new CrosshairSuppressionService();
        _deafSounds = new DeafSoundService(this);
        _deafSounds.Load();
        _fieldOfView = new FieldOfViewService();
        _tracker = new TrackerTrailService(this, Config.Tracker);
        _tracker.Load();
        _mindHack = new MindHackService(this);
        _mindHack.Load();
        _ninjaVisibility = new NinjaVisibilityService();
        PluginText.Configure(_illiterate);
        _registry = SkillRegistry.CreateDefault(
            Config,
            _explosions,
            _barrels,
            _fortnite,
            _illusionist,
            _longRangeWeapons,
            _grapple,
            _throwingKnives,
            _fireRain,
            _friendlyFire,
            _playerView,
            _noRecoil,
            _bombMiner,
            _wallhack,
            _nightmare,
            _darkness,
            _homingGrenades,
            _spectator,
            _illiterate,
            _thirdEye,
            _falconEye,
            _cypher,
            _revives,
            _ghosts,
            _chickens,
            _healingChickens,
            _specialHeart,
            _helpingHand,
            _findThem,
            _kamikazeChickens,
            _glaz,
            _holyHandGrenades,
            _crosshairs,
            _deafSounds,
            _fieldOfView,
            _tracker,
            _mindHack,
            _ninjaVisibility,
            _navMesh);
        _eventRegistry = EventRegistry.CreateDefault(Config, _wallhack, _deafSounds, _navMesh, _fog, _playerView, _chickens, _helpingHand, _explosions);
        var performance = new PerformanceMonitor(this);
        _skillManager = new SkillManager(this, _registry, performance);
        _silentSounds = new SilentSoundService(this, _skillManager);
        _silentSounds.Load();
        _roundEventManager = new RoundEventManager(this, _eventRegistry, performance);
        _presentation = new RoundPresentationService(this, _skillManager);
        _roundCoordinator = new RoundCoordinator(this, _skillManager, _roundEventManager, _presentation);
        _eventRouter = new SkillEventRouter(this, _skillManager, _roundEventManager, _wallhack, _nightmare, _darkness, _illiterate, _ghosts, _chickens, _glaz, _presentation, _crosshairs, _tracker, _ninjaVisibility);
        _damageEventRouter = new DamageEventRouter(this, _skillManager, _explosions, _roundEventManager);
        _damageEventRouter.Load();

        RegisterEventHandler<EventRoundStart>(_roundCoordinator.OnRoundStart, HookMode.Post);
        RegisterEventHandler<EventRoundStart>(_explosions.OnRoundStart, HookMode.Pre);
        RegisterEventHandler<EventRoundStart>(_fireRain.OnRoundStart, HookMode.Pre);
        RegisterEventHandler<EventRoundStart>(_friendlyFire.OnRoundStart, HookMode.Pre);
        RegisterEventHandler<EventRoundEnd>(_roundCoordinator.OnRoundEnd, HookMode.Post);
        RegisterEventHandler<EventPlayerHurt>(_eventRouter.OnPlayerHurtPre, HookMode.Pre);
        RegisterEventHandler<EventPlayerHurt>(_eventRouter.OnPlayerHurt, HookMode.Post);
        RegisterEventHandler<EventPlayerDeath>(_eventRouter.OnPlayerDeath, HookMode.Post);
        RegisterEventHandler<EventPlayerJump>(_eventRouter.OnPlayerJump, HookMode.Post);
        RegisterEventHandler<EventPlayerBlind>(_eventRouter.OnPlayerBlind, HookMode.Post);
        RegisterEventHandler<EventFlashbangDetonate>(_eventRouter.OnFlashbangDetonate, HookMode.Post);
        RegisterEventHandler<EventWeaponFire>(_eventRouter.OnWeaponFire, HookMode.Post);
        RegisterEventHandler<EventWeaponReload>(_eventRouter.OnWeaponReload, HookMode.Post);
        RegisterEventHandler<EventBulletImpact>(_eventRouter.OnBulletImpact, HookMode.Post);
        RegisterEventHandler<EventPlayerDeath>(_explosions.OnPlayerDeathPre, HookMode.Pre);
        RegisterEventHandler<EventDecoyStarted>(_eventRouter.OnDecoyStarted, HookMode.Post);
        RegisterEventHandler<EventDecoyDetonate>(_eventRouter.OnDecoyDetonate, HookMode.Post);
        RegisterEventHandler<EventGrenadeThrown>(_eventRouter.OnGrenadeThrown, HookMode.Post);
        RegisterEventHandler<EventSmokegrenadeDetonate>(_eventRouter.OnSmokeDetonate, HookMode.Post);
        RegisterEventHandler<EventSmokegrenadeExpired>(_eventRouter.OnSmokeExpired, HookMode.Post);
        RegisterEventHandler<EventRoundStart>(_glaz.OnRoundStart, HookMode.Pre);
        RegisterEventHandler<EventPlayerSpawn>(_eventRouter.OnPlayerSpawn, HookMode.Post);
        RegisterEventHandler<EventItemPickup>(_eventRouter.OnItemPickup, HookMode.Post);
        RegisterEventHandler<EventPlayerDisconnect>(_eventRouter.OnPlayerDisconnect, HookMode.Post);
        RegisterListener<Listeners.OnTick>(_eventRouter.OnTick);
        RegisterListener<Listeners.OnEntitySpawned>(_eventRouter.OnEntitySpawned);
        RegisterListener<Listeners.CheckTransmit>(_eventRouter.OnCheckTransmit);
        RegisterListener<Listeners.OnPlayerButtonsChanged>(_eventRouter.OnPlayerButtonsChanged);
        RegisterListener<Listeners.OnServerPrecacheResources>(_nightmare.OnServerPrecacheResources);
        AddCommand("css_rskill_status", "Show random-skill plugin status", OnStatusCommand);
        AddCommand("css_useskill", "Activate your current active skill", OnUseSkillCommand);
        AddCommand("css_forceevent", "Force the next round event from server console", OnForceEventCommand);
        AddCommand("css_nav_status", "Show current static NavMesh load status", OnNavStatusCommand);
        AddCommand("css_nav_randomtp", "Admin test: safely teleport yourself to a random reachable NAV area", OnNavRandomTeleportCommand);

        Logger.LogInformation(
            "{Plugin} loaded ({SkillCount} registered skills, hotReload={HotReload})",
            ModuleName,
            _registry.Count,
            hotReload);
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        _wasdMenus?.CloseAll();
        _navMesh?.Unload();
        _silentSounds?.Unload();
        _damageEventRouter?.Unload();
        _noRecoil?.Reset();
        _bombMiner?.Unload();
        _holyHandGrenades?.Unload();
        _friendlyFire?.Reset();
        _fireRain?.Unload();
        _barrels?.Unload();
        _fortnite?.Unload();
        _illusionist?.Unload();
        _grapple?.Unload();
        _throwingKnives?.Unload();
        _explosions?.Unload();
        _roundCoordinator?.CancelPendingAssignment();
        _presentation?.Clear();
        _skillManager?.RevokeAll(clearSessions: true);
        _roundEventManager?.EndRound();
        _wallhack?.Dispose();
        _nightmare?.Dispose();
        _darkness?.Dispose();
        _homingGrenades?.Unload();
        _spectator?.Unload();
        _thirdEye?.Unload();
        _falconEye?.Unload();
        _cypher?.Unload();
        _ghosts?.Dispose();
        _chickens?.Dispose();
        _healingChickens?.Dispose();
        _specialHeart?.Dispose();
        _findThem?.Dispose();
        _kamikazeChickens?.Dispose();
        _glaz?.Dispose();
        _crosshairs?.Dispose();
        _deafSounds?.Unload();
        _fieldOfView?.Dispose();
        _tracker?.Unload();
        _mindHack?.Unload();
        _ninjaVisibility?.Dispose();
        PluginText.Reset();
        Logger.LogInformation("{Plugin} unloaded (hotReload={HotReload})", ModuleName, hotReload);
    }

    private static void OnServerPrecacheResources(ResourceManifest manifest) =>
        manifest.AddResource("models/props_junk/watermelon01.vmdl");

    private void OnStatusCommand(CCSPlayerController? player, CommandInfo command)
    {
        command.ReplyToCommand(
            $"[Myrt1eSkill_Remake] enabled={Config.Enabled}, skills={_registry.Count}, events={_eventRegistry.Count}, activeEvents=[{string.Join(",", _roundEventManager.ActiveEventIds)}], players={_skillManager.AssignedPlayerCount}, assignments={_skillManager.ActiveAssignmentCount}");
    }

    private void OnUseSkillCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            command.ReplyToCommand("This command can only be used by a player.");
            return;
        }

        if (!_skillManager.TryActivate(player))
        {
            command.ReplyToCommand("[Myrt1eSkill_Remake] 当前没有可使用的主动技能。");
        }
    }

    private void OnForceEventCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is not null)
        {
            command.ReplyToCommand("[Myrt1eSkill_Remake] 该调试命令只能从服务器控制台使用。");
            return;
        }

        if (command.ArgCount < 2)
        {
            command.ReplyToCommand("Usage: css_forceevent <EventId>");
            return;
        }

        var eventId = command.GetArg(1);
        command.ReplyToCommand(_roundEventManager.ForceNextEvent(eventId)
            ? $"[Myrt1eSkill_Remake] Next round event forced to {eventId}."
            : $"[Myrt1eSkill_Remake] Unknown event: {eventId}.");
    }

    private void OnNavStatusCommand(CCSPlayerController? player, CommandInfo command)
    {
        var source = string.IsNullOrWhiteSpace(_navMesh.Source) ? "-" : _navMesh.Source;
        var error = string.IsNullOrWhiteSpace(_navMesh.LastError) ? "-" : _navMesh.LastError;
        command.ReplyToCommand(
            $"[Myrt1eSkill_Remake] navReady={_navMesh.IsReady}, map={_navMesh.MapName}, areas={_navMesh.AreaCount}, source={source}, error={error}");
    }

    private void OnNavRandomTeleportCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null)
        {
            command.ReplyToCommand("This command must be used by an in-game admin.");
            return;
        }

        if (player.IsBot || !AdminManager.PlayerHasPermissions(player, "@css/generic"))
        {
            command.ReplyToCommand("[Myrt1eSkill_Remake] You do not have permission to test NavMesh teleport.");
            return;
        }

        if (!_navMesh.TryTeleportRandom(player, out var failure))
        {
            command.ReplyToCommand($"[Myrt1eSkill_Remake] NavMesh teleport failed: {failure}");
            return;
        }

        command.ReplyToCommand("[Myrt1eSkill_Remake] Safe NavMesh teleport completed.");
    }
}
