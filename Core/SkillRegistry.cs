using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Skills;

namespace Myrt1eSkill_Remake.Core;

public sealed class SkillRegistry
{
    private readonly Dictionary<string, ISkill> _skills = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _skills.Count;
    public IReadOnlyCollection<ISkill> All => _skills.Values;

    public static SkillRegistry CreateDefault(
        PluginConfig config,
        ExplosiveProjectileService explosions,
        ExplodingBarrelService barrels,
        FireRainService fireRain,
        FriendlyFireService friendlyFire,
        PlayerViewService playerView,
        NoRecoilService noRecoil,
        BombMinerService bombMiner,
        WallhackService wallhack,
        NightmareService nightmare,
        IlliterateService illiterate,
        ThirdEyeService thirdEye,
        ReviveService revives,
        GhostService ghosts,
        ChickenService chickens,
        GlazService glaz,
        HolyHandGrenadeService holyHandGrenades)
    {
        var registry = new SkillRegistry();
        var antiFlash = new AntiFlashSkill(config.AntiFlash);
        registry.Register(new FleetFootedSkill());
        registry.Register(new VampiricRoundsSkill());
        registry.Register(new FieldMedicSkill());
        registry.Register(new ArmoredSkill(config.Armored));
        registry.Register(new IronHeadSkill());
        registry.Register(new DwarfSkill(config.Dwarf));
        registry.Register(new EnemySpinSkill(config.EnemySpin, playerView));
        registry.Register(new FireRainSkill(fireRain));
        registry.Register(new DashSkill(config.Dash));
        registry.Register(new FriendlyFireSkill(config.FriendlyFire, friendlyFire));
        registry.Register(new FrozenDecoySkill(config.FrozenDecoy));
        registry.Register(new ExplodingBarrelSkill(barrels));
        registry.Register(new EnemySpawnSkill());
        registry.Register(new OneShotSkill());
        registry.Register(new NoRecoilSkill(noRecoil));
        registry.Register(new ProsthesisSkill());
        registry.Register(new QuickShotSkill());
        registry.Register(new RamboSkill(config.Rambo));
        registry.Register(new RadarHackSkill());
        registry.Register(new ToxicSmokeSkill(config.ToxicSmoke));
        registry.Register(new PilotSkill(config.Pilot));
        registry.Register(new MeitoSkill());
        registry.Register(new BombMinerSkill(config.BombMiner, bombMiner));
        registry.Register(new SoundMakerSkill(config.SoundMaker));
        registry.Register(new ThirdEyeSkill(thirdEye));
        registry.Register(new TimeRecallSkill(config.TimeRecall, playerView));
        registry.Register(new TimeControllerSkill(config.TimeController));
        registry.Register(new MuhammadSkill(config.Muhammad, explosions));
        registry.Register(new DisarmSkill(config.Disarm));
        registry.Register(new KillerFlashSkill(config.KillerFlash, antiFlash));
        registry.Register(new PhoenixSkill(config.Phoenix, revives));
        registry.Register(new SecondChanceSkill(config.SecondChance, revives));
        registry.Register(new GhostSkill(ghosts));
        registry.Register(antiFlash);
        registry.Register(new ChickenSkill(chickens));
        registry.Register(new FlashJumpSkill(config.FlashJump, antiFlash));
        registry.Register(new GlazSkill(config.Glaz, glaz));
        registry.Register(new HolyHandGrenadeSkill(config.HolyHandGrenade, holyHandGrenades));
        registry.Register(new ExplosiveShotSkill(config.ExplosiveShot, explosions));
        registry.Register(new WallhackSkill(wallhack));
        registry.Register(new NightmareSkill(nightmare));
        registry.Register(new IlliterateSkill(illiterate));
        return registry;
    }

    public void Register(ISkill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        var id = skill.Descriptor.Id;

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("A skill must have a non-empty id.");
        }

        if (!_skills.TryAdd(id, skill))
        {
            throw new InvalidOperationException($"Duplicate skill id: {id}");
        }
    }

    public bool TryGet(string id, out ISkill? skill) => _skills.TryGetValue(id, out skill);
}
