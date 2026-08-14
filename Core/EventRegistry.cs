using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Events;

namespace Myrt1eSkill_Remake.Core;

public sealed class EventRegistry
{
    private readonly Dictionary<string, IRoundEvent> _events = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _events.Count;
    public IReadOnlyCollection<IRoundEvent> All => _events.Values;

    public static EventRegistry CreateDefault(
        PluginConfig config,
        WallhackService wallhack,
        DeafSoundService deafSounds,
        NavMeshService navMesh,
        FogService fog,
        PlayerViewService playerView,
        ChickenService chickens,
        HelpingHandService helpingHand,
        ExplosiveProjectileService explosions)
    {
        var registry = new EventRegistry();
        registry.Register(new NormalRoundEvent());
        registry.Register(new NoSkillEvent());
        registry.Register(new MoreSkillsEvent());
        registry.Register(new SkillsPlusPlusEvent());
        registry.Register(new ChooseCarnivalEvent(config.ChooseCarnivalSkillId));
        registry.Register(new FastBunnyHopEvent(config.FastBunnyHop));
        registry.Register(new LowGravityEvent());
        registry.Register(new LowGravityPlusPlusEvent());
        registry.Register(new JumpOnShootEvent());
        registry.Register(new JumpPlusPlusEvent());
        registry.Register(new BlitzkriegEvent());
        registry.Register(new SlowMotionEvent());
        registry.Register(new SwapOnHitEvent());
        registry.Register(new HurtTeleportEvent(navMesh));
        registry.Register(new DecoyTeleportEvent());
        registry.Register(new ChickenModeEvent());
        registry.Register(new BankruptcyEvent());
        registry.Register(new InfiniteAmmoModeEvent());
        registry.Register(new DeadlyGrenadesEvent(config.DeadlyGrenades));
        registry.Register(new BirdshotKingEvent());
        registry.Register(new ReloadTeleportEvent(navMesh));
        registry.Register(new StrangerEvent(navMesh));
        registry.Register(new CarefulShotEvent());
        registry.Register(new SmallButDeadlyEvent(config.SmallButDeadly));
        registry.Register(new InfiniteColoredSmokeEvent());
        registry.Register(new UnluckyCouplesEvent(config.UnluckyCouples, wallhack));
        registry.Register(new SuperKnockbackEvent(config.SuperKnockback));
        registry.Register(new SuperRecoilEvent(config.SuperRecoil));
        registry.Register(new InaccurateEvent(config.Inaccurate));
        registry.Register(new SilentWorldEvent(deafSounds));
        registry.Register(new AnywhereBombPlantEvent());
        registry.Register(new KillerSatelliteEvent());
        registry.Register(new ExplosionsAreArtEvent());
        registry.Register(new SkillMasterEvent());
        registry.Register(new RainyDayEvent(config.RainyDay));
        registry.Register(new SuperpowerXrayEvent(wallhack));
        registry.Register(new XrayEvent(wallhack));
        registry.Register(new TopTierPartyEvent());
        registry.Register(new TopTierPartyPlusPlusEvent());
        registry.Register(new MistAroundEvent(fog));
        registry.Register(new FindHimEvent(config.FindHimEvent, navMesh, wallhack));
        registry.Register(new NeckTiltEvent(config.NeckTiltEvent, playerView));
        registry.Register(new BigHeadEvent(config.BigHeadEvent));
        registry.Register(new FairDuelEvent());
        registry.Register(new FragileEveryoneEvent());
        registry.Register(new HelpingHandEvent(helpingHand));
        registry.Register(new BombardmentZoneEvent(config.BombardmentZone, explosions, navMesh));
        registry.Register(new WeaponRouletteEvent(config.WeaponRoulette));
        registry.Register(new KingModeEvent(config.KingMode, wallhack));
        registry.Register(new StandingStillBombsEvent(config.StandingStillBombs, explosions));
        registry.Register(new GiantEvent(config.Giant));
        registry.Register(new ManyChickensEvent(config.ManyChickensEvent, config.Chicken, navMesh, chickens));
        return registry;
    }

    public void Register(IRoundEvent roundEvent)
    {
        ArgumentNullException.ThrowIfNull(roundEvent);
        var id = roundEvent.Descriptor.Id;
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("An event must have a non-empty id.");
        }

        if (!_events.TryAdd(id, roundEvent))
        {
            throw new InvalidOperationException($"Duplicate event id: {id}");
        }
    }

    public bool TryGet(string id, out IRoundEvent? roundEvent) => _events.TryGetValue(id, out roundEvent);
}
