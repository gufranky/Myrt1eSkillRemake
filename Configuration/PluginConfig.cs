using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Configuration;

public sealed class SkillOverrideConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("Weight")]
    public int? Weight { get; set; }

    [JsonPropertyName("Rarity")]
    public string? Rarity { get; set; }

    [JsonPropertyName("MaxPerServer")]
    public int? MaxPerServer { get; set; }
}

public sealed class EventOverrideConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("Weight")]
    public int? Weight { get; set; }
}

public sealed class FastBunnyHopSettings
{
    [JsonPropertyName("AirAccelerate")]
    public float AirAccelerate { get; set; } = 100.0f;
}

public sealed class ArmoredSettings
{
    [JsonPropertyName("MinimumDamageMultiplier")]
    public float MinimumDamageMultiplier { get; set; } = 0.65f;

    [JsonPropertyName("MaximumDamageMultiplier")]
    public float MaximumDamageMultiplier { get; set; } = 0.85f;
}

public sealed class ExplosiveShotSettings
{
    [JsonPropertyName("Damage")]
    public float Damage { get; set; } = 25.0f;

    [JsonPropertyName("DamageRadius")]
    public float DamageRadius { get; set; } = 210.0f;

    [JsonPropertyName("MinimumChance")]
    public float MinimumChance { get; set; } = 0.15f;

    [JsonPropertyName("MaximumChance")]
    public float MaximumChance { get; set; } = 0.30f;

    [JsonPropertyName("TeammateDamageReduction")]
    public float TeammateDamageReduction { get; set; } = 0.50f;
}

public sealed class NightmareSettings
{
    [JsonPropertyName("PostProcessing")]
    public string PostProcessing { get; set; } = "lighting/postprocessing/effects/death_cam_phase1_low_violence.vpost";

    [JsonPropertyName("FadeTime")]
    public float FadeTime { get; set; } = 0.25f;

    [JsonPropertyName("MinimumExposure")]
    public float MinimumExposure { get; set; } = 0.50f;

    [JsonPropertyName("MaximumExposure")]
    public float MaximumExposure { get; set; } = 2.0f;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 0.50f;
}

public sealed class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("SkillsPerPlayer")]
    public int SkillsPerPlayer { get; set; } = 1;

    [JsonPropertyName("MaxSkillsPerPlayer")]
    public int MaxSkillsPerPlayer { get; set; } = 4;

    [JsonPropertyName("MaxActiveSkillsPerPlayer")]
    public int MaxActiveSkillsPerPlayer { get; set; } = 1;

    [JsonPropertyName("SkillTimeBeforeStart")]
    public float SkillTimeBeforeStart { get; set; } = 7.0f;

    [JsonPropertyName("SkillHudDuration")]
    public float SkillHudDuration { get; set; } = -1.0f;

    [JsonPropertyName("SkillDescriptionDuration")]
    public float SkillDescriptionDuration { get; set; } = 7.0f;

    [JsonPropertyName("YourSkillChatInfo")]
    public bool YourSkillChatInfo { get; set; } = true;

    [JsonPropertyName("TeamMateSkillChatInfo")]
    public bool TeamMateSkillChatInfo { get; set; } = true;

    [JsonPropertyName("RepeatBlockRounds")]
    public int RepeatBlockRounds { get; set; } = 4;

    [JsonPropertyName("ActivateWithUseKey")]
    public bool ActivateWithUseKey { get; set; } = true;

    [JsonPropertyName("RarityWeights")]
    public Dictionary<string, int> RarityWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Common"] = 70,
        ["Uncommon"] = 14,
        ["Rare"] = 10,
        ["Epic"] = 5,
        ["Legendary"] = 1
    };

    [JsonPropertyName("Skills")]
    public Dictionary<string, SkillOverrideConfig> Skills { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FleetFooted"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["VampiricRounds"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Rare",
            MaxPerServer = 4
        },
        ["FieldMedic"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Uncommon",
            MaxPerServer = 4
        },
        ["Armored"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["ExplosiveShot"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Wallhack"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Epic",
            MaxPerServer = 1
        },
        ["Nightmare"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Rare",
            MaxPerServer = -1
        },
        ["Illiterate"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = 1
        }
    };

    [JsonPropertyName("Armored")]
    public ArmoredSettings Armored { get; set; } = new();

    [JsonPropertyName("ExplosiveShot")]
    public ExplosiveShotSettings ExplosiveShot { get; set; } = new();

    [JsonPropertyName("Nightmare")]
    public NightmareSettings Nightmare { get; set; } = new();

    [JsonPropertyName("EventsEnabled")]
    public bool EventsEnabled { get; set; } = true;

    [JsonPropertyName("MaxEventsPerRound")]
    public int MaxEventsPerRound { get; set; } = 4;

    [JsonPropertyName("EventRepeatBlockRounds")]
    public int EventRepeatBlockRounds { get; set; } = 4;

    [JsonPropertyName("ChooseCarnivalSkillId")]
    public string ChooseCarnivalSkillId { get; set; } = "FieldMedic";

    [JsonPropertyName("FastBunnyHop")]
    public FastBunnyHopSettings FastBunnyHop { get; set; } = new();

    [JsonPropertyName("Events")]
    public Dictionary<string, EventOverrideConfig> Events { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NormalRound"] = new() { Enabled = true, Weight = 100 },
        ["NoSkill"] = new() { Enabled = true, Weight = 10 },
        ["MoreSkills"] = new() { Enabled = true, Weight = 10 },
        ["SkillsPlusPlus"] = new() { Enabled = true, Weight = 5 },
        ["ChooseCarnival"] = new() { Enabled = true, Weight = 5 },
        ["FastBunnyHop"] = new() { Enabled = true, Weight = 8 },
        ["LowGravity"] = new() { Enabled = true, Weight = 10 },
        ["LowGravityPlusPlus"] = new() { Enabled = true, Weight = 10 },
        ["JumpOnShoot"] = new() { Enabled = true, Weight = 10 },
        ["JumpPlusPlus"] = new() { Enabled = true, Weight = 10 },
        ["Blitzkrieg"] = new() { Enabled = true, Weight = 10 },
        ["SlowMotion"] = new() { Enabled = true, Weight = 10 },
        ["SwapOnHit"] = new() { Enabled = true, Weight = 10 },
        ["DecoyTeleport"] = new() { Enabled = true, Weight = 10 },
        ["ChickenMode"] = new() { Enabled = true, Weight = 10 },
        ["SuperpowerXray"] = new() { Enabled = true, Weight = 10 },
        ["Xray"] = new() { Enabled = true, Weight = 10 },
        ["TopTierParty"] = new() { Enabled = true, Weight = 3 },
        ["TopTierPartyPlusPlus"] = new() { Enabled = true, Weight = 1 }
    };

    [JsonPropertyName("PerformanceLoggingEnabled")]
    public bool PerformanceLoggingEnabled { get; set; }

    [JsonPropertyName("PerformanceReportSeconds")]
    public float PerformanceReportSeconds { get; set; } = 5.0f;

    [JsonPropertyName("PerformanceWarningMilliseconds")]
    public double PerformanceWarningMilliseconds { get; set; } = 1.0;
}
