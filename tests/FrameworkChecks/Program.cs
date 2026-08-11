using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;
using Myrt1eSkill_Remake.Events;
using Myrt1eSkill_Remake.Skills;

var checks = new List<(string Name, Action Run)>
{
    ("MoreSkills and SkillsPlusPlus resolve to three slots", CheckSkillSlotMerge),
    ("NoSkill overrides forced skill directives", CheckNoSkillPrecedence),
    ("NoSkill and ChooseCarnival are incompatible", CheckEventConflict),
    ("ChooseCarnival produces ReplaceAll plan", CheckReplaceAll),
    ("Round plan clamps slots and active skill count", CheckPlanLimits),
    ("SpeedBoost is an exclusive 50-percent movement skill", CheckSpeedBoostDescriptor),
    ("DeathNote is a one-use active mutual-suicide skill", CheckDeathNoteDescriptor),
    ("Duplicator is an active assignment-replacement skill", CheckDuplicatorDescriptor),
    ("Deactivator is an epic one-use assignment removal skill", CheckDeactivatorDescriptor),
    ("ChooseOneOfThree replaces itself from three candidates", CheckChooseOneOfThreeDescriptor),
    ("RangeFinder provides passive distance and targeted vision", CheckRangeFinderDescriptor),
    ("InfiniteAmmo refills fire, reload and grenade paths", CheckInfiniteAmmoRouting),
    ("FastBunnyHop blocks player jump-control skills", CheckFastBunnyHopCompatibility),
    ("Low-gravity variants cannot be combined", CheckLowGravityConflict),
    ("LowGravityPlusPlus blocks gravity and spread skills", CheckLowGravityPlusPlusCompatibility),
    ("Jump events consume weapon-fire callbacks", CheckJumpEventRouting),
    ("Jump event variants cannot be combined", CheckJumpEventConflict),
    ("Spread-changing events cannot be combined", CheckSpreadEventConflict),
    ("Time-scale events cannot be combined", CheckTimeScaleConflict),
    ("SwapOnHit consumes hurt and tick callbacks", CheckSwapOnHitRouting),
    ("DecoyTeleport consumes decoy and spawn callbacks", CheckDecoyTeleportRouting),
    ("ChickenMode consumes complete visual lifecycle callbacks", CheckChickenModeRouting),
    ("ChickenMode blocks movement speed skills", CheckChickenModeCompatibility),
    ("Bankruptcy resets the economy to 800 dollars", CheckBankruptcyDescriptor),
    ("InfiniteAmmoMode owns and blocks global ammo rules", CheckInfiniteAmmoModeDescriptor),
    ("DeadlyGrenades owns the HE-only loadout and economy rules", CheckDeadlyGrenadesDescriptor),
    ("SmallButDeadly owns scale speed and health rules", CheckSmallButDeadlyDescriptor),
    ("InfiniteColoredSmoke consumes grenade and entity callbacks", CheckInfiniteColoredSmokeRouting),
    ("UnluckyCouples consumes pre-damage callbacks", CheckUnluckyCouplesRouting),
    ("SuperKnockback consumes player-hurt callbacks", CheckSuperKnockbackRouting),
    ("SuperRecoil consumes weapon-fire callbacks", CheckSuperRecoilRouting),
    ("Skill state bags isolate assignment state", CheckSkillStateIsolation),
    ("Armored consumes the pre-damage pipeline", CheckArmoredRouting),
    ("IronHead consumes victim pre-hurt callbacks", CheckIronHeadRouting),
    ("Dwarf controls player scale", CheckDwarfDescriptor),
    ("EnemySpin consumes player-hurt callbacks", CheckEnemySpinRouting),
    ("FireRain consumes decoy-started callbacks", CheckFireRainRouting),
    ("Dash consumes tick callbacks", CheckDashRouting),
    ("FriendlyFire consumes attacker pre-damage callbacks", CheckFriendlyFireRouting),
    ("FrozenDecoy consumes its complete decoy lifecycle", CheckFrozenDecoyRouting),
    ("DecoyXRay is a passive targeted reveal skill", CheckDecoyXRayDescriptor),
    ("ExplodingBarrel is a reusable active placement skill", CheckExplodingBarrelDescriptor),
    ("EnemySpawn is an active teleport skill", CheckEnemySpawnDescriptor),
    ("OneShot consumes attacker pre-damage callbacks", CheckOneShotRouting),
    ("NoRecoil consumes tick callbacks", CheckNoRecoilRouting),
    ("Prosthesis consumes victim pre-hurt callbacks", CheckProsthesisRouting),
    ("QuickShot consumes tick callbacks", CheckQuickShotRouting),
    ("Rambo owns max-health changes", CheckRamboDescriptor),
    ("RadarHack consumes tick callbacks", CheckRadarHackRouting),
    ("ToxicSmoke consumes its complete smoke lifecycle", CheckToxicSmokeRouting),
    ("Pilot consumes tick callbacks", CheckPilotRouting),
    ("Meito consumes victim pre-damage callbacks", CheckMeitoRouting),
    ("BombMiner consumes grenade-thrown callbacks", CheckBombMinerRouting),
    ("SoundMaker emits enemy screams on tick", CheckSoundMakerRouting),
    ("ThirdEye toggles a zero-cooldown camera", CheckThirdEyeDescriptor),
    ("TimeRecall records history for active rewind", CheckTimeRecallDescriptor),
    ("TimeController owns global timescale", CheckTimeControllerDescriptor),
    ("Muhammad consumes player-death callbacks", CheckMuhammadRouting),
    ("Disarm consumes player-hurt callbacks", CheckDisarmRouting),
    ("KillerFlash consumes player-blind callbacks", CheckKillerFlashRouting),
    ("Phoenix consumes lethal pre-damage callbacks", CheckPhoenixRouting),
    ("SecondChance consumes one lethal pre-damage callback", CheckSecondChanceRouting),
    ("Ghost consumes hurt and death callbacks", CheckGhostRouting),
    ("AntiFlash consumes player-blind callbacks", CheckAntiFlashRouting),
    ("Chicken controls model, scale and movement", CheckChickenRouting),
    ("FlashJump consumes blind and flash-detonate callbacks", CheckFlashJumpRouting),
    ("Glaz replenishes smoke grenades", CheckGlazRouting),
    ("HolyHandGrenade replenishes enhanced HE grenades", CheckHolyHandGrenadeRouting),
    ("KillInvincibility consumes kill and pre-damage callbacks", CheckKillInvincibilityRouting),
    ("ExplosiveShot consumes bullet-impact callbacks", CheckExplosiveShotRouting),
    ("Wallhack is a passive visibility skill", CheckWallhackDescriptor),
    ("Xray variants cannot be combined", CheckXrayEventConflict),
    ("Xray events block outline skills and model events", CheckXrayCompatibility),
    ("SuperpowerXray handles disconnect replacement", CheckSuperpowerRouting),
    ("Nightmare is a one-use active vision debuff", CheckNightmareDescriptor),
    ("Darkness uses the reference persistent black Fade", CheckDarknessDescriptor),
    ("HomingNades tracks non-smoke projectiles and replenishes grenades", CheckHomingNadesDescriptor),
    ("Spectator toggles a zero-cooldown enemy camera", CheckSpectatorDescriptor),
    ("BlastShot launches native HE projectiles from MP5 secondary fire", CheckBlastShotDescriptor),
    ("Flashlight toggles a blinding barn light", CheckFlashlightDescriptor),
    ("Fortnite places destructible barricades", CheckFortniteDescriptor),
    ("Grapple traces an anchor and pulls the player", CheckGrappleDescriptor),
    ("JumpCurse mirrors holder jumps to grounded enemies", CheckJumpCurseRouting),
    ("Pusher rolls an on-hit enemy knockback chance", CheckPusherRouting),
    ("ThrowingKnife launches the holder's stealable lethal knife", CheckThrowingKnifeDescriptor),
    ("Jammer is an active crosshair suppression skill", CheckJammerDescriptor),
    ("Illiterate scrambles letters and digits", CheckIlliterateScramble),
    ("Skill reveal timing matches jRandomSkills", CheckRevealTiming),
    ("Skill presentation defaults match jRandomSkills", CheckPresentationDefaults)
};

foreach (var (name, run) in checks)
{
    run();
    Console.WriteLine($"PASS: {name}");
}

Console.WriteLine($"Framework checks passed: {checks.Count}/{checks.Count}");

static void CheckSkillSlotMerge()
{
    var config = NewConfig();
    var more = new MoreSkillsEvent();
    var plusPlus = new SkillsPlusPlusEvent();
    var builder = new RoundPlanBuilder(config);
    builder.SetActiveEvents(new IRoundEvent[] { more, plusPlus });
    more.Contribute(builder);
    plusPlus.Contribute(builder);
    var plan = builder.Build();

    Assert(plan.Skills.SlotsPerPlayer == 3, "Expected max slot requirement to be 3.");
}

static void CheckNoSkillPrecedence()
{
    var config = NewConfig();
    var noSkill = new NoSkillEvent();
    var choose = new ChooseCarnivalEvent("FieldMedic");
    var builder = new RoundPlanBuilder(config);
    builder.SetActiveEvents(new IRoundEvent[] { noSkill, choose });
    choose.Contribute(builder);
    noSkill.Contribute(builder);
    var plan = builder.Build();

    Assert(!plan.Skills.Enabled, "NoSkill must disable the skill plan.");
    Assert(plan.Skills.ForcedMode == ForcedSkillMode.None, "Disabled plan must clear forced mode.");
}

static void CheckEventConflict()
{
    var noSkill = new NoSkillEvent();
    var choose = new ChooseCarnivalEvent("FieldMedic");
    Assert(!CompatibilityResolver.CanCombine(new IRoundEvent[] { noSkill }, choose), "Exclusive event tags must conflict.");
}

static void CheckReplaceAll()
{
    var config = NewConfig();
    var choose = new ChooseCarnivalEvent("FieldMedic");
    var builder = new RoundPlanBuilder(config);
    builder.SetActiveEvents(new IRoundEvent[] { choose });
    choose.Contribute(builder);
    var plan = builder.Build();

    Assert(plan.Skills.ForcedMode == ForcedSkillMode.ReplaceAll, "Expected ReplaceAll mode.");
    Assert(plan.Skills.ForcedSkillIds.SequenceEqual(new[] { "FieldMedic" }), "Expected forced FieldMedic skill.");
    Assert(plan.Skills.SlotsPerPlayer == 1, "ReplaceAll should use the exact forced skill count.");
}

static void CheckPlanLimits()
{
    var config = NewConfig();
    config.SkillsPerPlayer = 99;
    config.MaxSkillsPerPlayer = 4;
    config.MaxActiveSkillsPerPlayer = 1;
    var builder = new RoundPlanBuilder(config);
    builder.SetActiveEvents(Array.Empty<IRoundEvent>());
    var plan = builder.Build();

    Assert(plan.Skills.SlotsPerPlayer == 4, "Skill slots must be clamped to configured maximum.");
    Assert(plan.Skills.MaxActiveSkillsPerPlayer == 1, "Active skill cap must be preserved.");
}

static void CheckFastBunnyHopCompatibility()
{
    var config = NewConfig();
    var bunnyHop = new FastBunnyHopEvent(config.FastBunnyHop);
    var builder = new RoundPlanBuilder(config);
    builder.SetActiveEvents(new IRoundEvent[] { bunnyHop });
    var plan = builder.Build();

    Assert(plan.Skills.BlockedSkillTags.Contains("jump-control"),
        "FastBunnyHop must block player skills that also control jumping.");
}

static void CheckLowGravityConflict()
{
    var lowGravity = new LowGravityEvent();
    var plusPlus = new LowGravityPlusPlusEvent();
    Assert(!CompatibilityResolver.CanCombine(new IRoundEvent[] { lowGravity }, plusPlus),
        "Low-gravity variants must share an exclusive event tag.");
}

static void CheckLowGravityPlusPlusCompatibility()
{
    var config = NewConfig();
    var plusPlus = new LowGravityPlusPlusEvent();
    var builder = new RoundPlanBuilder(config);
    builder.SetActiveEvents(new IRoundEvent[] { plusPlus });
    var plan = builder.Build();

    Assert(plan.Skills.BlockedSkillTags.Contains("gravity-control"),
        "LowGravityPlusPlus must block gravity-changing skills.");
    Assert(plan.Skills.BlockedSkillTags.Contains("weapon-spread-control"),
        "LowGravityPlusPlus must block spread-changing skills.");
}

static void CheckJumpEventRouting()
{
    Assert(new JumpOnShootEvent() is IRoundEventWeaponFire,
        "JumpOnShoot must consume the typed weapon-fire event.");
    Assert(new JumpPlusPlusEvent() is IRoundEventWeaponFire,
        "JumpPlusPlus must consume the typed weapon-fire event.");
}

static void CheckJumpEventConflict()
{
    var jumpOnShoot = new JumpOnShootEvent();
    var plusPlus = new JumpPlusPlusEvent();
    Assert(!CompatibilityResolver.CanCombine(new IRoundEvent[] { jumpOnShoot }, plusPlus),
        "JumpOnShoot and JumpPlusPlus must share an exclusive event tag.");
}

static void CheckSpreadEventConflict()
{
    var lowGravityPlusPlus = new LowGravityPlusPlusEvent();
    var jumpPlusPlus = new JumpPlusPlusEvent();
    Assert(!CompatibilityResolver.CanCombine(new IRoundEvent[] { lowGravityPlusPlus }, jumpPlusPlus),
        "Events that both own weapon spread must be incompatible.");
}

static void CheckTimeScaleConflict()
{
    var blitzkrieg = new BlitzkriegEvent();
    var slowMotion = new SlowMotionEvent();
    Assert(!CompatibilityResolver.CanCombine(new IRoundEvent[] { blitzkrieg }, slowMotion),
        "Blitzkrieg and SlowMotion must share an exclusive time-scale tag.");
}

static void CheckSwapOnHitRouting()
{
    var swap = new SwapOnHitEvent();
    Assert(swap is IRoundEventPlayerHurt, "SwapOnHit must consume player-hurt callbacks.");
    Assert(swap is IRoundEventTick, "SwapOnHit must consume tick callbacks for cooldown cleanup.");
}

static void CheckDecoyTeleportRouting()
{
    var teleport = new DecoyTeleportEvent();
    Assert(teleport is IRoundEventDecoyStarted,
        "DecoyTeleport must consume decoy-started callbacks.");
    Assert(teleport is IRoundEventPlayerSpawn,
        "DecoyTeleport must replenish decoys after player spawns.");
}

static void CheckChickenModeRouting()
{
    var chicken = new ChickenModeEvent();
    Assert(chicken is IRoundEventPlayerSpawn,
        "ChickenMode must reapply after player spawns.");
    Assert(chicken is IRoundEventItemPickup,
        "ChickenMode must hide newly picked-up third-person weapons.");
    Assert(chicken is IRoundEventCheckTransmit,
        "ChickenMode must control third-person entity visibility.");
}

static void CheckChickenModeCompatibility()
{
    var config = NewConfig();
    var chicken = new ChickenModeEvent();
    var builder = new RoundPlanBuilder(config);
    builder.SetActiveEvents(new IRoundEvent[] { chicken });
    var plan = builder.Build();

    Assert(plan.Skills.BlockedSkillTags.Contains("movement-speed"),
        "ChickenMode must block skills that also own player movement speed.");
}

static void CheckSkillStateIsolation()
{
    var first = new SkillStateBag();
    var second = new SkillStateBag();
    first.Set(new TestState { Value = 0.7f });
    second.Set(new TestState { Value = 0.8f });

    Assert(first.TryGet<TestState>(out var firstState) && firstState!.Value == 0.7f,
        "First assignment must retain its own typed state.");
    Assert(second.TryGet<TestState>(out var secondState) && secondState!.Value == 0.8f,
        "Second assignment must retain an independent typed state.");
}

static void CheckArmoredRouting()
{
    var armored = new ArmoredSkill(new ArmoredSettings());
    Assert(armored is IPreDamageSkill,
        "Armored must modify damage through the pre-damage pipeline.");
    Assert(armored.Descriptor.Kind == SkillKind.Passive,
        "Armored must be a passive skill.");
}

static void CheckInfiniteColoredSmokeRouting()
{
    var coloredSmoke = new InfiniteColoredSmokeEvent();
    Assert(coloredSmoke is IRoundEventGrenadeThrown
           && coloredSmoke is IRoundEventPlayerSpawn
           && coloredSmoke is IRoundEventEntitySpawned,
        "InfiniteColoredSmoke must replenish grenades and color spawned smoke projectiles.");
    Assert(coloredSmoke.Descriptor.BlockedSkillTags.Contains("smoke-behavior-control"),
        "InfiniteColoredSmoke must block per-player smoke behavior overrides.");
}

static void CheckSpeedBoostDescriptor()
{
    var speedBoost = new SpeedBoostSkill();
    var fleetFooted = new FleetFootedSkill();
    Assert(speedBoost.Descriptor.Kind == SkillKind.Passive,
        "SpeedBoost must be a passive skill.");
    Assert(speedBoost.Descriptor.Description.Contains("50%", StringComparison.Ordinal),
        "SpeedBoost must advertise the reference 50-percent increase.");
    Assert(speedBoost.Descriptor.ConflictTags.Overlaps(fleetFooted.Descriptor.ConflictTags),
        "SpeedBoost must not stack with other movement-speed owners.");
}

static void CheckDeathNoteDescriptor()
{
    var deathNote = new DeathNoteSkill();
    Assert(deathNote.Descriptor.Kind == SkillKind.Active,
        "DeathNote must open its target menu through active-skill input.");
    Assert(deathNote.Descriptor.CooldownSeconds == 0,
        "DeathNote must rely on its one-use state instead of a reusable cooldown.");
    Assert(deathNote.Descriptor.ConflictTags.Contains("mutual-suicide"),
        "DeathNote must declare ownership of mutual-suicide resolution.");
}

static void CheckDuplicatorDescriptor()
{
    var duplicator = new DuplicatorSkill();
    Assert(duplicator.Descriptor.Kind == SkillKind.Active,
        "Duplicator must open enemy selection through active-skill input.");
    Assert(duplicator.Descriptor.CooldownSeconds == 0,
        "Duplicator must replace itself instead of becoming reusable on cooldown.");
    Assert(duplicator.Descriptor.ConflictTags.Contains("skill-assignment-replacement"),
        "Duplicator must declare runtime ownership of its skill assignment.");
}

static void CheckDeactivatorDescriptor()
{
    var deactivator = new DeactivatorSkill();
    Assert(deactivator.Descriptor.Kind == SkillKind.Active,
        "Deactivator must require target selection through active-skill input.");
    Assert(deactivator.Descriptor.Rarity == SkillRarity.Epic,
        "Deactivator must use the requested epic rarity.");
    Assert(deactivator.Descriptor.CooldownSeconds == 0.0f,
        "Deactivator is consumed on success and does not need a cooldown.");
    Assert(deactivator.Descriptor.ConflictTags.Contains("skill-assignment-replacement"),
        "Deactivator must not overlap another assignment-replacing skill.");
}

static void CheckChooseOneOfThreeDescriptor()
{
    var chooser = new ChooseOneOfThreeSkill();
    Assert(chooser.Descriptor.Kind == SkillKind.Active,
        "ChooseOneOfThree must open its selection menu through active-skill input.");
    Assert(chooser.Descriptor.ConflictTags.Contains("skill-assignment-replacement"),
        "ChooseOneOfThree must replace its runtime assignment.");
    Assert(new PluginConfig().ChooseCarnivalSkillId == "ChooseOneOfThree",
        "ChooseCarnival must force the completed chooser skill by default.");
}

static void CheckRangeFinderDescriptor()
{
    var rangeFinder = new RangeFinderSkill(new RangeFinderSettings(), null!);
    Assert(rangeFinder.Descriptor.Kind == SkillKind.Passive && rangeFinder is ITickSkill,
        "RangeFinder must update distance continuously as a passive skill.");
    var settings = new RangeFinderSettings();
    Assert(settings.XrayDistanceThreshold == 500
           && settings.UnitsPerMeter == 100
           && settings.UpdateIntervalSeconds == 0.15f,
        "RangeFinder defaults must preserve the original five-meter scan behavior.");
}

static void CheckInfiniteAmmoRouting()
{
    var infiniteAmmo = new InfiniteAmmoSkill();
    Assert(infiniteAmmo.Descriptor.Kind == SkillKind.Passive,
        "InfiniteAmmo must work without active-skill input.");
    Assert(infiniteAmmo is IWeaponFireSkill
           && infiniteAmmo is IWeaponReloadSkill
           && infiniteAmmo is IGrenadeThrownSkill,
        "InfiniteAmmo must refill firearms and replace thrown grenades.");
    Assert(InfiniteAmmoSkill.RefilledClipAmmo == 100,
        "InfiniteAmmo must preserve jRandomSkills' 100-round magazine behavior.");
}

static void CheckBankruptcyDescriptor()
{
    var bankruptcy = new BankruptcyEvent();
    Assert(bankruptcy.Descriptor.Id == "Bankruptcy",
        "Bankruptcy must keep the original event id.");
    Assert(BankruptcyEvent.BankruptcyMoney == 800,
        "Bankruptcy must reset every account to 800 dollars.");
    Assert(bankruptcy.Descriptor.ExclusiveTags.Contains("economy-reset"),
        "Bankruptcy must own the one-shot economy reset operation.");
}

static void CheckInfiniteAmmoModeDescriptor()
{
    var infiniteAmmo = new InfiniteAmmoModeEvent();
    Assert(infiniteAmmo.Descriptor.Id == "InfiniteAmmoMode",
        "The global event must use an id distinct from the per-player InfiniteAmmo skill.");
    Assert(InfiniteAmmoModeEvent.InfiniteAmmoValue == 1,
        "sv_infinite_ammo 1 must provide ammunition without requiring reloads.");
    Assert(infiniteAmmo.Descriptor.ExclusiveTags.Contains("global-ammo-rules"),
        "InfiniteAmmoMode must own the global ammunition rule.");
    Assert(infiniteAmmo.Descriptor.BlockedSkillTags.Contains("weapon-ammo-control"),
        "InfiniteAmmoMode must suppress redundant per-player ammunition skills.");
}

static void CheckDeadlyGrenadesDescriptor()
{
    var settings = new DeadlyGrenadesSettings();
    var deadlyGrenades = new DeadlyGrenadesEvent(settings);
    var infiniteAmmo = new InfiniteAmmoModeEvent();
    Assert(deadlyGrenades is IRoundEventPlayerSpawn
           && deadlyGrenades is IRoundEventGrenadeThrown
           && deadlyGrenades is IRoundEventItemPickup,
        "DeadlyGrenades must enforce its loadout on spawn, throw and pickup paths.");
    Assert(settings.DamageMultiplier == 3.0f
           && settings.RadiusMultiplier == 5.0f
           && settings.StartingGrenadeCount == 3,
        "DeadlyGrenades must preserve the original Myrt1eSkill defaults.");
    Assert(DeadlyGrenadesEvent.BuyAllowGunsValue == 0 && DeadlyGrenadesEvent.BuyTimeValue == 0.0f,
        "DeadlyGrenades must disable gun purchases and the buy window.");
    Assert(deadlyGrenades.Descriptor.ExclusiveTags.Contains("global-ammo-rules")
           && !CompatibilityResolver.CanCombine(new[] { infiniteAmmo }, deadlyGrenades),
        "DeadlyGrenades must not combine with another global infinite-ammo event.");
    Assert(deadlyGrenades.Descriptor.BlockedSkillTags.Contains("weapon-ammo-control")
           && deadlyGrenades.Descriptor.BlockedSkillTags.Contains("hegrenade-behavior-control"),
        "DeadlyGrenades must suppress redundant ammo and HE behavior skills.");
}

static void CheckSmallButDeadlyDescriptor()
{
    var settings = new SmallButDeadlySettings();
    var smallButDeadly = new SmallButDeadlyEvent(settings);
    var chickenMode = new ChickenModeEvent();
    Assert(smallButDeadly is IRoundEventPlayerSpawn,
        "SmallButDeadly must reapply its attributes after every spawn.");
    Assert(settings.PlayerScale == 0.50f
           && settings.SpeedMultiplier == 2.0f
           && settings.Health == 10,
        "SmallButDeadly must preserve the requested scale, speed and health values.");
    Assert(smallButDeadly.Descriptor.ExclusiveTags.Contains("player-scale-rules")
           && smallButDeadly.Descriptor.ExclusiveTags.Contains("movement-speed-rules")
           && smallButDeadly.Descriptor.ExclusiveTags.Contains("player-health-rules"),
        "SmallButDeadly must own all three global attribute rules.");
    Assert(!CompatibilityResolver.CanCombine(new[] { chickenMode }, smallButDeadly),
        "SmallButDeadly must not combine with ChickenMode.");
    Assert(smallButDeadly.Descriptor.BlockedSkillTags.Contains("movement-speed")
           && smallButDeadly.Descriptor.BlockedSkillTags.Contains("player-scale-control")
           && smallButDeadly.Descriptor.BlockedSkillTags.Contains("max-health-control"),
        "SmallButDeadly must block conflicting player attribute skills.");
}

static void CheckUnluckyCouplesRouting()
{
    var couples = new UnluckyCouplesEvent(new UnluckyCouplesSettings(), null!);
    Assert(couples is IRoundEventPreDamage,
        "UnluckyCouples must multiply damage through the event pre-damage pipeline.");
    Assert(couples.Descriptor.ExclusiveTags.Contains("xray-vision-rules"),
        "UnluckyCouples must conflict with global xray events.");
    Assert(couples.Descriptor.BlockedSkillTags.Contains("player-outline-vision"),
        "UnluckyCouples must preserve partner-only visibility.");
}

static void CheckSuperKnockbackRouting()
{
    var knockback = new SuperKnockbackEvent(new SuperKnockbackSettings());
    Assert(knockback is IRoundEventPlayerHurt,
        "SuperKnockback must react only after real player damage is reported.");
    Assert(knockback.Descriptor.ExclusiveTags.Contains("damage-knockback-rules"),
        "SuperKnockback must own global damage knockback rules.");
}

static void CheckSuperRecoilRouting()
{
    var recoil = new SuperRecoilEvent(new SuperRecoilSettings());
    Assert(recoil is IRoundEventWeaponFire,
        "SuperRecoil must react to weapon-fire callbacks.");
    Assert(recoil.Descriptor.ExclusiveTags.Contains("weapon-recoil-force-rules"),
        "SuperRecoil must own global physical recoil rules.");
}

static void CheckIronHeadRouting()
{
    var ironHead = new IronHeadSkill();
    Assert(ironHead is IPlayerHurtPreSkill,
        "IronHead must suppress headshots through the victim pre-hurt pipeline.");
    Assert(ironHead.Descriptor.Kind == SkillKind.Passive,
        "IronHead must be a passive skill.");
    Assert(ironHead.Descriptor.Rarity == SkillRarity.Common,
        "IronHead must preserve the reference common rarity.");
}

static void CheckDwarfDescriptor()
{
    var dwarf = new DwarfSkill(new DwarfSettings());
    Assert(dwarf.Descriptor.Kind == SkillKind.Passive,
        "Dwarf must be a passive skill.");
    Assert(dwarf.Descriptor.Rarity == SkillRarity.Common,
        "Dwarf must preserve the reference common rarity.");
    Assert(dwarf.Descriptor.ConflictTags.Contains("player-scale-control"),
        "Dwarf must declare ownership of player scale.");
}

static void CheckEnemySpinRouting()
{
    var enemySpin = new EnemySpinSkill(new EnemySpinSettings(), null!);
    Assert(enemySpin is IPlayerHurtSkill,
        "EnemySpin must react to successful hits.");
    Assert(enemySpin.Descriptor.Rarity == SkillRarity.Common,
        "EnemySpin must preserve the reference common rarity.");
}

static void CheckFireRainRouting()
{
    Assert(typeof(IDecoyStartedSkill).IsAssignableFrom(typeof(FireRainSkill)),
        "FireRain must consume decoy-started callbacks.");
}

static void CheckDashRouting()
{
    var dash = new DashSkill(new DashSettings());
    Assert(dash is ITickSkill,
        "Dash must inspect jump input through the tick pipeline.");
    Assert(dash.Descriptor.ConflictTags.Contains("jump-control"),
        "Dash must declare ownership of jump control.");
}

static void CheckFriendlyFireRouting()
{
    var friendlyFire = new FriendlyFireSkill(new FriendlyFireSettings(), new FriendlyFireService());
    Assert(friendlyFire is IPreDamageAttackerSkill,
        "FriendlyFire must transform damage through the attacker pre-damage pipeline.");
    Assert(friendlyFire.Descriptor.RequiresTeammate,
        "FriendlyFire must only be assigned when a teammate is available.");
}

static void CheckFrozenDecoyRouting()
{
    var frozenDecoy = new FrozenDecoySkill(new FrozenDecoySettings());
    Assert(frozenDecoy is ITickSkill,
        "FrozenDecoy must update proximity slowing through the tick pipeline.");
    Assert(frozenDecoy is IDecoyStartedSkill && frozenDecoy is IDecoyDetonateSkill,
        "FrozenDecoy must consume the complete active-decoy lifecycle.");
    Assert(frozenDecoy is IGrenadeThrownSkill,
        "FrozenDecoy must replenish its configured decoy charges after throws.");
}

static void CheckDecoyXRayDescriptor()
{
    var skill = new DecoyXRaySkill(new DecoyXRaySettings(), null!);
    Assert(skill.Descriptor.Kind == SkillKind.Passive,
        "DecoyXRay must grant its grenades without active-skill input.");
    Assert(skill is IGrenadeThrownSkill && skill is IDecoyDetonateSkill,
        "DecoyXRay must replenish thrown decoys and reveal targets on detonation.");
    Assert(skill.Descriptor.ConflictTags.Contains("decoy-behavior-control")
           && skill.Descriptor.ConflictTags.Contains("player-outline-vision")
           && skill.Descriptor.ConflictTags.Contains("radar-vision"),
        "DecoyXRay must conflict with other decoy and vision controllers.");

    var settings = new DecoyXRaySettings();
    Assert(settings.GrenadeCount == 3
           && settings.RevealRadius == 500
           && settings.RevealDurationSeconds == 10,
        "DecoyXRay defaults must preserve the reference behavior.");
}

static void CheckExplodingBarrelDescriptor()
{
    var barrel = new ExplodingBarrelSkill(null!);
    Assert(barrel.Descriptor.Kind == SkillKind.Active,
        "ExplodingBarrel must use the active-skill pipeline.");
    Assert(barrel.Descriptor.CooldownSeconds == 20.0f,
        "ExplodingBarrel must preserve the reference 20-second cooldown.");
    Assert(barrel.Descriptor.MaxPerServer == 2,
        "ExplodingBarrel must preserve the reference server limit.");
}

static void CheckEnemySpawnDescriptor()
{
    var enemySpawn = new EnemySpawnSkill();
    Assert(enemySpawn.Descriptor.Kind == SkillKind.Active,
        "EnemySpawn must use the active-skill pipeline.");
    Assert(enemySpawn.Descriptor.CooldownSeconds == 15.0f,
        "EnemySpawn must preserve the reference 15-second cooldown.");
    Assert(enemySpawn.Descriptor.ConflictTags.Contains("player-teleport-control"),
        "EnemySpawn must declare ownership of player teleportation.");
}

static void CheckOneShotRouting()
{
    var oneShot = new OneShotSkill();
    Assert(oneShot is IPreDamageAttackerSkill,
        "OneShot must override damage through the attacker pre-damage pipeline.");
    Assert(oneShot.Descriptor.Kind == SkillKind.Passive,
        "OneShot must be a passive skill.");
}

static void CheckNoRecoilRouting()
{
    var noRecoil = new NoRecoilSkill(new NoRecoilService());
    Assert(noRecoil is ITickSkill,
        "NoRecoil must clear recoil through the tick pipeline.");
    Assert(noRecoil.Descriptor.ConflictTags.Contains("weapon-spread-control"),
        "NoRecoil must conflict with global no-spread events.");
}

static void CheckProsthesisRouting()
{
    var prosthesis = new ProsthesisSkill();
    Assert(prosthesis is IPlayerHurtPreSkill,
        "Prosthesis must suppress limb hits through the victim pre-hurt pipeline.");
}

static void CheckQuickShotRouting()
{
    var quickShot = new QuickShotSkill();
    Assert(quickShot is ITickSkill,
        "QuickShot must continuously release weapon attack timers.");
    Assert(quickShot.Descriptor.ConflictTags.Contains("weapon-fire-rate-control"),
        "QuickShot must declare ownership of weapon fire rate.");
}

static void CheckRamboDescriptor()
{
    var rambo = new RamboSkill(new RamboSettings());
    Assert(rambo.Descriptor.ConflictTags.Contains("max-health-control"),
        "Rambo must declare ownership of maximum health.");
}

static void CheckRadarHackRouting()
{
    var radarHack = new RadarHackSkill();
    Assert(radarHack is ITickSkill,
        "RadarHack must refresh spotted masks through the tick pipeline.");
    Assert(radarHack.Descriptor.ConflictTags.Contains("radar-vision"),
        "RadarHack must declare ownership of radar visibility.");
}

static void CheckToxicSmokeRouting()
{
    var toxicSmoke = new ToxicSmokeSkill(new ToxicSmokeSettings());
    Assert(toxicSmoke is ITickSkill,
        "ToxicSmoke must apply area damage through the tick pipeline.");
    Assert(toxicSmoke is ISmokeDetonateSkill && toxicSmoke is ISmokeExpiredSkill,
        "ToxicSmoke must consume the complete active-smoke lifecycle.");
    Assert(toxicSmoke is IGrenadeThrownSkill,
        "ToxicSmoke must track its configured smoke-grenade charges.");
}

static void CheckPilotRouting()
{
    var pilot = new PilotSkill(new PilotSettings());
    Assert(pilot is ITickSkill,
        "Pilot must consume hold-to-fly input through the tick pipeline.");
    Assert(pilot.Descriptor.ConflictTags.Contains("flight-control"),
        "Pilot must declare ownership of player flight.");
}

static void CheckMeitoRouting()
{
    var meito = new MeitoSkill();
    Assert(meito is IPreDamageSkill,
        "Meito must cancel lethal damage through the victim pre-damage pipeline.");
    Assert(meito.Descriptor.ConflictTags.Contains("second-chance"),
        "Meito must conflict with other second-chance skills.");
}

static void CheckBombMinerRouting()
{
    var miner = new BombMinerSkill(new BombMinerSettings(), null!);
    Assert(miner is IGrenadeThrownSkill,
        "BombMiner must track its configured HE grenade charges.");
    Assert(miner.Descriptor.ConflictTags.Contains("hegrenade-behavior-control"),
        "BombMiner must declare ownership of HE grenade behavior.");
}

static void CheckSoundMakerRouting()
{
    var soundMaker = new SoundMakerSkill(new SoundMakerSettings());
    Assert(soundMaker is ITickSkill,
        "SoundMaker must schedule screams through the tick pipeline.");
    Assert(soundMaker.Descriptor.Kind == SkillKind.Passive,
        "SoundMaker must be a passive skill.");
    Assert(soundMaker.Descriptor.Rarity == SkillRarity.Common,
        "SoundMaker must preserve the reference common rarity.");
}

static void CheckThirdEyeDescriptor()
{
    var thirdEye = new ThirdEyeSkill(null!);
    Assert(thirdEye is ITickSkill,
        "ThirdEye must update its camera through the tick pipeline.");
    Assert(thirdEye.Descriptor.Kind == SkillKind.Active,
        "ThirdEye must use the active-skill pipeline.");
    Assert(thirdEye.Descriptor.CooldownSeconds == 0.0f,
        "ThirdEye must preserve the reference zero-second cooldown.");
    Assert(thirdEye.Descriptor.ConflictTags.Contains("camera-view-control"),
        "ThirdEye must declare ownership of the player camera.");
}

static void CheckTimeRecallDescriptor()
{
    var timeRecall = new TimeRecallSkill(new TimeRecallSettings(), null!);
    Assert(timeRecall is ITickSkill,
        "TimeRecall must record player history through the tick pipeline.");
    Assert(timeRecall.Descriptor.Kind == SkillKind.Active,
        "TimeRecall must use the active-skill pipeline.");
    Assert(timeRecall.Descriptor.CooldownSeconds == 15.0f,
        "TimeRecall must preserve the reference 15-second cooldown.");
    Assert(timeRecall.Descriptor.ConflictTags.Contains("player-teleport-control"),
        "TimeRecall must declare ownership of player teleportation.");
}

static void CheckTimeControllerDescriptor()
{
    var controller = new TimeControllerSkill(new TimeControllerSettings());
    Assert(controller.Descriptor.Kind == SkillKind.Active,
        "TimeController must use the active-skill pipeline.");
    Assert(controller.Descriptor.CooldownSeconds == 0.1f,
        "TimeController must preserve the reference 0.1-second cooldown.");
    Assert(controller.Descriptor.ConflictTags.Contains("timescale-control"),
        "TimeController must conflict with global timescale events.");
}

static void CheckMuhammadRouting()
{
    var muhammad = new MuhammadSkill(new MuhammadSettings(), null!);
    Assert(muhammad is IPlayerDeathSkill,
        "Muhammad must trigger through the player-death pipeline.");
    Assert(muhammad.Descriptor.Kind == SkillKind.Passive,
        "Muhammad must be a passive skill.");
    Assert(muhammad.Descriptor.Rarity == SkillRarity.Common,
        "Muhammad must use the common rarity.");
}

static void CheckDisarmRouting()
{
    var disarm = new DisarmSkill(new DisarmSettings());
    Assert(disarm is IPlayerHurtSkill,
        "Disarm must trigger after a successful enemy hit.");
    Assert(disarm.Descriptor.Kind == SkillKind.Passive,
        "Disarm must be a passive skill.");
    Assert(disarm.Descriptor.Rarity == SkillRarity.Common,
        "Disarm must preserve the reference common rarity.");
}

static void CheckKillerFlashRouting()
{
    var killerFlash = new KillerFlashSkill(new KillerFlashSettings());
    Assert(killerFlash is IPlayerBlindSkill,
        "KillerFlash must trigger through the player-blind pipeline.");
    Assert(killerFlash.Descriptor.Kind == SkillKind.Passive,
        "KillerFlash must be a passive skill.");
    Assert(killerFlash.Descriptor.Rarity == SkillRarity.Epic,
        "KillerFlash must preserve the reference epic rarity.");
    Assert(killerFlash.Descriptor.MaxPerServer == 1,
        "KillerFlash must preserve the reference server limit.");
}

static void CheckPhoenixRouting()
{
    var phoenix = new PhoenixSkill(new PhoenixSettings(), null!);
    Assert(phoenix is IPreDamageSkill,
        "Phoenix must intercept lethal damage before death is committed.");
    Assert(phoenix.Descriptor.ConflictTags.Contains("second-chance"),
        "Phoenix must conflict with other resurrection skills.");
}

static void CheckSecondChanceRouting()
{
    var secondChance = new SecondChanceSkill(new SecondChanceSettings(), null!);
    Assert(secondChance is IPreDamageSkill,
        "SecondChance must intercept lethal damage before death is committed.");
    Assert(secondChance.Descriptor.ConflictTags.Contains("second-chance"),
        "SecondChance must conflict with other resurrection skills.");
}

static void CheckGhostRouting()
{
    var ghost = new GhostSkill(null!);
    Assert(ghost is IPlayerHurtSkill && ghost is IPlayerDeathSkill,
        "Ghost must reveal through hurt and death callbacks.");
    Assert(ghost.Descriptor.Rarity == SkillRarity.Epic,
        "Ghost must preserve the reference epic rarity.");
    Assert(ghost.Descriptor.ConflictTags.Contains("player-visibility-control"),
        "Ghost must declare ownership of player visibility.");
}

static void CheckAntiFlashRouting()
{
    var antiFlash = new AntiFlashSkill(new AntiFlashSettings());
    Assert(antiFlash is IPlayerBlindSkill,
        "AntiFlash must trigger through the player-blind pipeline.");
    Assert(antiFlash.Descriptor.Rarity == SkillRarity.Common,
        "AntiFlash must preserve the reference common rarity.");
    Assert(antiFlash.Descriptor.ConflictTags.Contains("flashbang-behavior-control"),
        "AntiFlash must conflict with other flashbang-changing skills.");
}

static void CheckChickenRouting()
{
    var chicken = new ChickenSkill(null!);
    Assert(chicken is ITickSkill && chicken is IPlayerDeathSkill,
        "Chicken must maintain its speed and clean up its model on death.");
    Assert(chicken.Descriptor.Rarity == SkillRarity.Common,
        "Chicken must preserve the reference common rarity.");
    Assert(chicken.Descriptor.ConflictTags.Contains("player-model-control")
           && chicken.Descriptor.ConflictTags.Contains("movement-speed"),
        "Chicken must declare ownership of player model and movement speed.");
}

static void CheckFlashJumpRouting()
{
    var flashJump = new FlashJumpSkill(new FlashJumpSettings());
    Assert(flashJump is IPlayerBlindSkill && flashJump is IFlashbangDetonateSkill,
        "FlashJump must consume blind and flashbang-detonate callbacks.");
    Assert(flashJump.Descriptor.ConflictTags.Contains("flashbang-behavior-control"),
        "FlashJump must conflict with other flashbang behavior skills.");
}

static void CheckGlazRouting()
{
    var glaz = new GlazSkill(new GlazSettings(), null!);
    Assert(glaz is IGrenadeThrownSkill,
        "Glaz must replenish its configured smoke-grenade charges.");
    Assert(glaz.Descriptor.ConflictTags.Contains("smoke-behavior-control"),
        "Glaz must conflict with other smoke behavior skills.");
}

static void CheckHolyHandGrenadeRouting()
{
    var holyGrenade = new HolyHandGrenadeSkill(new HolyHandGrenadeSettings(), null!);
    Assert(holyGrenade is IGrenadeThrownSkill,
        "HolyHandGrenade must replenish after HE grenade throws.");
    Assert(holyGrenade.Descriptor.ConflictTags.Contains("hegrenade-behavior-control"),
        "HolyHandGrenade must conflict with other HE grenade behavior skills.");
    Assert(holyGrenade.Descriptor.Rarity == SkillRarity.Common,
        "HolyHandGrenade must preserve the reference common rarity.");
}

static void CheckKillInvincibilityRouting()
{
    var invincibility = new KillInvincibilitySkill(new KillInvincibilitySettings());
    Assert(invincibility is IPlayerDeathSkill && invincibility is IPreDamageSkill,
        "KillInvincibility must activate on kills and cancel damage before it is applied.");
    Assert(invincibility is ITickSkill,
        "KillInvincibility must expire and notify through the tick pipeline.");
    Assert(invincibility.Descriptor.Rarity == SkillRarity.Common,
        "KillInvincibility must use the common rarity.");
}

static void CheckExplosiveShotRouting()
{
    Assert(typeof(IBulletImpactSkill).IsAssignableFrom(typeof(ExplosiveShotSkill)),
        "ExplosiveShot must consume the typed bullet-impact event.");
}

static void CheckWallhackDescriptor()
{
    Assert(typeof(ISkill).IsAssignableFrom(typeof(WallhackSkill)),
        "Wallhack must participate in the skill framework.");
}

static void CheckXrayEventConflict()
{
    Assert(!CompatibilityResolver.CanCombine(
            new IRoundEvent[] { new XrayEvent(null!) },
            new SuperpowerXrayEvent(null!)),
        "Xray and SuperpowerXray must share an exclusive vision tag.");
}

static void CheckXrayCompatibility()
{
    var xray = new XrayEvent(null!);
    var chicken = new ChickenModeEvent();
    Assert(!CompatibilityResolver.CanCombine(new IRoundEvent[] { chicken }, xray),
        "Xray must not combine with player-model replacement events.");
    Assert(xray.Descriptor.BlockedSkillTags.Contains("player-outline-vision"),
        "Xray must block redundant outline skills.");
}

static void CheckSuperpowerRouting()
{
    Assert(typeof(IRoundEventPlayerDisconnect).IsAssignableFrom(typeof(SuperpowerXrayEvent)),
        "SuperpowerXray must replace a selected player after disconnect.");
}

static void CheckNightmareDescriptor()
{
    var nightmare = new NightmareSkill(null!);
    Assert(nightmare.Descriptor.Kind == SkillKind.Active,
        "Nightmare must be activated before target selection.");
    Assert(nightmare.Descriptor.Rarity == SkillRarity.Rare,
        "Nightmare must preserve the reference rarity.");
    Assert(nightmare.Descriptor.ConflictTags.Contains("post-processing-vision"),
        "Nightmare must declare ownership of the target post-processing view.");
}

static void CheckDarknessDescriptor()
{
    var darkness = new DarknessSkill(null!);
    Assert(darkness.Descriptor.Kind == SkillKind.Active,
        "Darkness must require activation before selecting an enemy.");
    Assert(darkness.Descriptor.Rarity == SkillRarity.Rare,
        "Darkness must preserve jRandomSkills' rare rarity.");
    Assert(darkness.Descriptor.ConflictTags.Contains("screen-fade-vision"),
        "Darkness must declare ownership of the target screen-fade effect.");
    Assert(DarknessService.RefreshIntervalSeconds == 5.0f
           && DarknessService.FadeDuration == 100
           && DarknessService.FadeHoldTime == 3000,
        "Darkness must preserve the reference Fade timings.");
    Assert(DarknessService.PackColor(0, 0, 0, 230) == unchecked((int)0xE6000000),
        "Darkness must pack the reference RGBA(0,0,0,230) overlay correctly.");
}

static void CheckHomingNadesDescriptor()
{
    var settings = new HomingNadesSettings();
    var homing = new HomingNadesSkill(settings, null!);
    Assert(homing.Descriptor.Kind == SkillKind.Passive
           && homing.Descriptor.Rarity == SkillRarity.Common,
        "HomingNades must preserve the reference passive common classification.");
    Assert(homing is IGrenadeThrownSkill,
        "HomingNades must replenish its HE and flashbang charges after throws.");
    Assert(settings.Strength == 150.0f
           && settings.MaximumVelocity == 2000.0f
           && settings.DetonationRange == 130.0f,
        "HomingNades must preserve the reference steering defaults.");
    Assert(settings.HeGrenadeCount == 2 && settings.FlashbangCount == 2,
        "HomingNades must grant two HE grenades and two flashbangs.");
    Assert(HomingGrenadeService.TickStride == 10,
        "HomingNades must update projectile steering every ten ticks.");
}

static void CheckSpectatorDescriptor()
{
    var spectator = new SpectatorSkill(null!);
    Assert(spectator.Descriptor.Kind == SkillKind.Active
           && spectator.Descriptor.CooldownSeconds == 0.0f,
        "Spectator must be a zero-cooldown active camera toggle.");
    Assert(spectator is ITickSkill,
        "Spectator must maintain and validate its enemy camera every tick.");
    Assert(spectator.Descriptor.ConflictTags.Contains("camera-view-control"),
        "Spectator must not overlap another player camera controller.");
    Assert(new SpectatorSettings().Distance == 100.0f,
        "Spectator must preserve the reference 100-unit camera distance.");
    Assert(SpectatorCameraService.CameraModel == "models/sprays/spray_plane.vmdl",
        "Spectator must preserve the reference invisible camera model.");
}

static void CheckBlastShotDescriptor()
{
    var settings = new BlastShotSettings();
    var blastShot = new BlastShotSkill(settings, null!);
    Assert(blastShot is IPlayerButtonsChangedSkill && blastShot is ITickSkill,
        "BlastShot must consume Attack2 input and maintain its cooldown HUD.");
    Assert(blastShot.Descriptor.CooldownSeconds == 10.0f,
        "BlastShot must expose the requested ten-second cooldown.");
    Assert(BlastShotSkill.RequiredWeapon == "weapon_mp5sd",
        "BlastShot must only fire while the MP5-SD is active.");
    Assert(settings.ExplosionDamage == 60.0f
           && settings.ExplosionRadius == 400.0f
           && settings.Force == 1000.0f
           && settings.TeammateDamageMultiplier == 0.50f,
        "BlastShot must preserve the reference projectile defaults.");
}

static void CheckFlashlightDescriptor()
{
    var settings = new FlashlightSettings();
    var flashlight = new FlashlightSkill(settings, null!);
    Assert(flashlight is ITickSkill && flashlight is IPlayerDeathSkill,
        "Flashlight must update its light every tick and remove it on death.");
    Assert(flashlight.Descriptor.Kind == SkillKind.Active
           && flashlight.Descriptor.Rarity == SkillRarity.Legendary
           && flashlight.Descriptor.CooldownSeconds == 2.0f
           && flashlight.Descriptor.MaxPerServer == 2,
        "Flashlight must preserve the reference activation rules.");
    Assert(settings.Range == 1200.0f
           && settings.Brightness == 1.5f
           && settings.BlindDuration == 5.0f
           && settings.BlindAngle == 10.0f
           && settings.BlindAlpha == 200.0f,
        "Flashlight must preserve the reference beam and blindness defaults.");
}

static void CheckFortniteDescriptor()
{
    var settings = new FortniteSettings();
    var fortnite = new FortniteSkill(null!);
    Assert(fortnite.Descriptor.Kind == SkillKind.Active
           && fortnite.Descriptor.Rarity == SkillRarity.Common
           && fortnite.Descriptor.CooldownSeconds == 2.0f
           && fortnite.Descriptor.MaxPerServer == 5,
        "Fortnite must preserve the reference activation rules.");
    Assert(settings.BarricadeHealth == 115
           && settings.PlacementDistance == 50.0f
           && settings.SoundVolume == 1.0f
           && settings.PropModel.EndsWith("aztec_scaffold_wall_support_128.vmdl", StringComparison.Ordinal),
        "Fortnite must preserve the reference barricade defaults.");
}

static void CheckGrappleDescriptor()
{
    var settings = new GrappleSettings();
    var grapple = new GrappleSkill(settings, null!);
    Assert(grapple is ITickSkill && grapple is IPlayerDeathSkill,
        "Grapple must pull on tick and clean its rope on death.");
    Assert(grapple.Descriptor.Kind == SkillKind.Active
           && grapple.Descriptor.Rarity == SkillRarity.Rare
           && grapple.Descriptor.CooldownSeconds == 10.0f,
        "Grapple must be the requested ten-second rare active skill.");
    Assert(settings.MaximumDistance == 1500.0f
           && settings.MinimumDistance == 150.0f
           && settings.StopDistance == 90.0f
           && settings.PullSpeed == 850.0f
           && settings.MaximumPullSeconds == 3.0f,
        "Grapple must preserve the jRandomSkills movement defaults.");
    Assert(GrappleService.HookModel.EndsWith("grapplinghook_hook_01_open.vmdl", StringComparison.Ordinal),
        "Grapple must use the reference hook model.");
}

static void CheckJumpCurseRouting()
{
    var settings = new JumpCurseSettings();
    var jumpCurse = new JumpCurseSkill(settings);
    Assert(jumpCurse is IPlayerJumpSkill,
        "JumpCurse must consume the strongly typed player-jump route.");
    Assert(jumpCurse.Descriptor.Kind == SkillKind.Passive
           && jumpCurse.Descriptor.Rarity == SkillRarity.Common,
        "JumpCurse must be a common passive skill.");
    Assert(jumpCurse.Descriptor.ConflictTags.Contains("jump-control"),
        "JumpCurse must conflict with global or per-player jump controllers.");
    Assert(settings.JumpVelocity == 301.0f,
        "JumpCurse must preserve the reference forced-jump velocity.");
}

static void CheckPusherRouting()
{
    var settings = new PusherSettings();
    var pusher = new PusherSkill(settings);
    var superKnockback = new SuperKnockbackEvent(new SuperKnockbackSettings());
    Assert(pusher is IPlayerHurtSkill,
        "Pusher must trigger through the attacker player-hurt route.");
    Assert(pusher.Descriptor.Kind == SkillKind.Passive
           && pusher.Descriptor.Rarity == SkillRarity.Common,
        "Pusher must be a common passive skill.");
    Assert(settings.MinimumChance == 0.30f
           && settings.MaximumChance == 0.40f
           && settings.PushVelocity == 400.0f
           && settings.JumpVelocity == 300.0f,
        "Pusher must preserve the jRandomSkills probability and force defaults.");
    Assert(superKnockback.Descriptor.BlockedSkillTags.Contains("on-hit-knockback-control"),
        "SuperKnockback must block Pusher to prevent stacked knockback.");
}

static void CheckThrowingKnifeDescriptor()
{
    var settings = new ThrowingKnifeSettings();
    var throwingKnife = new ThrowingKnifeSkill(settings, null!);
    Assert(throwingKnife is ITickSkill && throwingKnife is IPlayerDeathSkill,
        "ThrowingKnife must detect recovery and clean a thrown knife on death.");
    Assert(throwingKnife.Descriptor.Kind == SkillKind.Active
           && throwingKnife.Descriptor.Rarity == SkillRarity.Common
           && throwingKnife.Descriptor.CooldownSeconds == 0.0f
           && throwingKnife.Descriptor.MaxPerServer == 1,
        "ThrowingKnife must preserve the reference active-skill limits.");
    Assert(settings.ThrowForce == 2000.0f
           && settings.TriggerRadius == 10.0f
           && settings.Damage == 9999.0f
           && !settings.FriendlyFire,
        "ThrowingKnife must preserve the reference force, hitbox, damage and friendly-fire defaults.");
    var deadlyGrenades = new DeadlyGrenadesEvent(new DeadlyGrenadesSettings());
    Assert(deadlyGrenades.Descriptor.BlockedSkillTags.Contains("projectile-launcher-control"),
        "DeadlyGrenades must block ThrowingKnife's projectile launcher rule.");
}

static void CheckJammerDescriptor()
{
    var jammer = new JammerSkill(null!);
    Assert(jammer.Descriptor.Kind == SkillKind.Active,
        "Jammer must require activation before selecting a target.");
    Assert(jammer.Descriptor.Rarity == SkillRarity.Common,
        "Jammer must preserve jRandomSkills' common rarity.");
    Assert(jammer is IPlayerDeathSkill,
        "Jammer must restore the crosshair when its target dies.");
    Assert(CrosshairSuppressionService.CrosshairHideHudBit == 1u << 8,
        "Jammer must use CS2's crosshair bit in m_iHideHUD.");
}

static void CheckIlliterateScramble()
{
    Assert(IlliterateService.Scramble("Abc 123!", 1) == "Bcd ???!",
        "Illiterate must Caesar-shift letters, replace digits, and preserve punctuation.");
    var skill = new IlliterateSkill(null!);
    Assert(skill.Descriptor.Kind == SkillKind.Passive && skill.Descriptor.MaxPerServer == 1,
        "Illiterate must be a server-limited passive skill.");
}

static void CheckRevealTiming()
{
    Assert(Math.Abs(RoundCoordinator.CalculateRevealDelay(15, 7, false) - 8.3f) < 0.001f,
        "A 15-second freeze must reveal at 8.3 seconds, about seven seconds before freeze end.");
    Assert(Math.Abs(RoundCoordinator.CalculateRevealDelay(15, 7, true) - 15.3f) < 0.001f,
        "Team intro must add the same seven-second offset used by jRandomSkills.");
}

static void CheckPresentationDefaults()
{
    var config = new PluginConfig();
    Assert(config.SkillTimeBeforeStart == 7, "Skill reveal lead time must default to seven seconds.");
    Assert(config.SkillHudDuration == -1, "Skill name HUD must persist by default.");
    Assert(config.SkillDescriptionDuration == 7, "Descriptions must remain for seven seconds.");
    Assert(config.YourSkillChatInfo && config.TeamMateSkillChatInfo,
        "Own and teammate skill chat announcements must be enabled by default.");
}


static PluginConfig NewConfig() => new()
{
    Enabled = true,
    SkillsPerPlayer = 1,
    MaxSkillsPerPlayer = 4,
    MaxActiveSkillsPerPlayer = 1
};

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class TestState
{
    public float Value { get; set; }
}
