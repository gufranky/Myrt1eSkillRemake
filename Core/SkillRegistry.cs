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
        FortniteService fortnite,
        IllusionistService illusionist,
        LongRangeWeaponService longRangeWeapons,
        GrappleService grapple,
        ThrowingKnifeService throwingKnives,
        FireRainService fireRain,
        FriendlyFireService friendlyFire,
        PlayerViewService playerView,
        NoRecoilService noRecoil,
        BombMinerService bombMiner,
        WallhackService wallhack,
        NightmareService nightmare,
        DarknessService darkness,
        HomingGrenadeService homingGrenades,
        SpectatorCameraService spectator,
        IlliterateService illiterate,
        ThirdEyeService thirdEye,
        FalconEyeService falconEye,
        CypherCameraService cypher,
        ReviveService revives,
        GhostService ghosts,
        ChickenService chickens,
        HealingChickenService healingChickens,
        SpecialHeartCompanionService specialHeart,
        HelpingHandService helpingHand,
        FindThemService findThem,
        KamikazeChickenService kamikazeChickens,
        GlazService glaz,
        HolyHandGrenadeService holyHandGrenades,
        CrosshairSuppressionService crosshairs,
        DeafSoundService deafSounds,
        SoundMakerService soundMaker,
        FieldOfViewService fieldOfView,
        TrackerTrailService tracker,
        MindHackService mindHack,
        NinjaVisibilityService ninjaVisibility,
        NavMeshService navMesh,
        PathTrailService pathTrails,
        SmokeProjectileService smokes)
    {
        var registry = new SkillRegistry();
        var antiFlash = new AntiFlashSkill(config.AntiFlash);
        registry.Register(new FleetFootedSkill());
        registry.Register(new SpeedBoostSkill());
        registry.Register(new DeathNoteSkill());
        registry.Register(new ZoneReaperSkill());
        registry.Register(new GhoulSkill(config.Ghoul));
        registry.Register(new MindHackSkill(config.MindHack, mindHack));
        registry.Register(new DuplicatorSkill());
        registry.Register(new DeactivatorSkill());
        registry.Register(new ChooseOneOfThreeSkill());
        registry.Register(new RangeFinderSkill(config.RangeFinder, wallhack));
        registry.Register(new InfiniteAmmoSkill());
        registry.Register(new VampiricRoundsSkill());
        registry.Register(new FieldMedicSkill());
        registry.Register(new ArmoredSkill(config.Armored));
        registry.Register(new ReactiveArmorSkill(config.ReactiveArmor));
        registry.Register(new BladeMasterSkill(config.BladeMaster));
        registry.Register(new IronHeadSkill());
        registry.Register(new DwarfSkill(config.Dwarf));
        registry.Register(new EnemySpinSkill(config.EnemySpin, playerView));
        registry.Register(new TakeOffSkill(config.TakeOff));
        registry.Register(new FireRainSkill(fireRain));
        registry.Register(new DashSkill(config.Dash));
        registry.Register(new FriendlyFireSkill(config.FriendlyFire, friendlyFire));
        registry.Register(new FrozenDecoySkill(config.FrozenDecoy));
        registry.Register(new MagneticDecoySkill(config.MagneticDecoy));
        registry.Register(new DecoyXRaySkill(config.DecoyXRay, wallhack));
        registry.Register(new ExplodingBarrelSkill(barrels));
        registry.Register(new FortniteSkill(fortnite));
        registry.Register(new GrappleSkill(config.Grapple, grapple));
        registry.Register(new JumpCurseSkill(config.JumpCurse));
        registry.Register(new PusherSkill(config.Pusher));
        registry.Register(new ThrowingKnifeSkill(config.ThrowingKnife, throwingKnives));
        registry.Register(new JumperSkill(config.Jumper));
        registry.Register(new EnemySpawnSkill());
        registry.Register(new HurtTeleportSkill(navMesh));
        registry.Register(new RandomTeleportSkill(navMesh));
        registry.Register(new WeaponSwapSkill());
        registry.Register(new DreadGazeSkill(config.DreadGaze));
        registry.Register(new AimbotSkill());
        registry.Register(new OneShotSkill());
        registry.Register(new NoRecoilSkill(noRecoil));
        registry.Register(new ProsthesisSkill());
        registry.Register(new QuickShotSkill());
        registry.Register(new RamboSkill(config.Rambo));
        registry.Register(new RadarHackSkill());
        registry.Register(new ToxicSmokeSkill(config.ToxicSmoke));
        registry.Register(new HealingSmokeSkill(config.HealingSmoke));
        registry.Register(new PyroSkill(config.Pyro));
        registry.Register(new RichBoySkill(config.RichBoy));
        registry.Register(new BountyHunterSkill(config.BountyHunter, wallhack));
        registry.Register(new ThornsSkill(config.Thorns));
        registry.Register(new GrenadierSkill());
        registry.Register(new NinjaSkill(config.Ninja, ninjaVisibility));
        registry.Register(new NinjaEscapeSkill(config.NinjaEscape, navMesh, smokes));
        registry.Register(new PilotSkill(config.Pilot));
        registry.Register(new MeitoSkill());
        registry.Register(new BombMinerSkill(config.BombMiner, bombMiner));
        registry.Register(new SoundMakerSkill(soundMaker));
        registry.Register(new SilentSkill());
        registry.Register(new ThirdEyeSkill(thirdEye));
        registry.Register(new FalconEyeSkill(falconEye));
        registry.Register(new CypherSkill(cypher));
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
        registry.Register(new SpecialHeartSkill(config.SpecialHeart, specialHeart));
        registry.Register(new HelpingHandSkill(helpingHand));
        registry.Register(new PathTrackerSkill(pathTrails));
        registry.Register(new HealingChickenSkill(healingChickens));
        registry.Register(new FindThemSkill(config.FindThem, findThem));
        registry.Register(new KamikazeChickenSkill(config.KamikazeChicken, kamikazeChickens));
        registry.Register(new FlashJumpSkill(config.FlashJump, antiFlash));
        registry.Register(new GlazSkill(config.Glaz, glaz));
        registry.Register(new HolyHandGrenadeSkill(config.HolyHandGrenade, holyHandGrenades));
        registry.Register(new KillInvincibilitySkill(config.KillInvincibility));
        registry.Register(new GodModeSkill(config.GodMode));
        registry.Register(new IllusionistSkill(illusionist));
        registry.Register(new LongKnifeSkill(config.LongKnife, longRangeWeapons));
        registry.Register(new LongZeusSkill(config.LongZeus, longRangeWeapons));
        registry.Register(new HotBombSkill(config.HotBomb));
        registry.Register(new MagnifierSkill(config.Magnifier, fieldOfView));
        registry.Register(new TrackerSkill(tracker));
        registry.Register(new ZrySkill());
        registry.Register(new AdaptiveDisguiseSkill());
        registry.Register(new ExplosiveShotSkill(config.ExplosiveShot, explosions));
        registry.Register(new WallhackSkill(wallhack));
        registry.Register(new NightmareSkill(nightmare));
        registry.Register(new DarknessSkill(darkness));
        registry.Register(new HomingNadesSkill(config.HomingNades, homingGrenades));
        registry.Register(new SpectatorSkill(spectator));
        registry.Register(new BlastShotSkill(config.BlastShot, explosions));
        registry.Register(new FlashlightSkill(config.Flashlight, antiFlash));
        registry.Register(new JammerSkill(crosshairs));
        registry.Register(new DeafSkill(deafSounds));
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
