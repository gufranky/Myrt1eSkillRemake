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
    ("Skill state bags isolate assignment state", CheckSkillStateIsolation),
    ("Armored consumes the pre-damage pipeline", CheckArmoredRouting),
    ("ExplosiveShot consumes bullet-impact callbacks", CheckExplosiveShotRouting),
    ("Wallhack is a passive visibility skill", CheckWallhackDescriptor),
    ("Xray variants cannot be combined", CheckXrayEventConflict),
    ("Xray events block outline skills and model events", CheckXrayCompatibility),
    ("SuperpowerXray handles disconnect replacement", CheckSuperpowerRouting),
    ("Nightmare is a one-use active vision debuff", CheckNightmareDescriptor),
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
