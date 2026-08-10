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
        DeafSoundService deafSounds)
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
        registry.Register(new DecoyTeleportEvent());
        registry.Register(new ChickenModeEvent());
        registry.Register(new BankruptcyEvent());
        registry.Register(new InfiniteAmmoModeEvent());
        registry.Register(new DeadlyGrenadesEvent(config.DeadlyGrenades));
        registry.Register(new SmallButDeadlyEvent(config.SmallButDeadly));
        registry.Register(new InfiniteColoredSmokeEvent());
        registry.Register(new UnluckyCouplesEvent(config.UnluckyCouples, wallhack));
        registry.Register(new SuperKnockbackEvent(config.SuperKnockback));
        registry.Register(new SuperRecoilEvent(config.SuperRecoil));
        registry.Register(new InaccurateEvent(config.Inaccurate));
        registry.Register(new SilentWorldEvent(deafSounds));
        registry.Register(new AnywhereBombPlantEvent());
        registry.Register(new KillerSatelliteEvent());
        registry.Register(new SkillMasterEvent());
        registry.Register(new RainyDayEvent(config.RainyDay));
        registry.Register(new SuperpowerXrayEvent(wallhack));
        registry.Register(new XrayEvent(wallhack));
        registry.Register(new TopTierPartyEvent());
        registry.Register(new TopTierPartyPlusPlusEvent());
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
