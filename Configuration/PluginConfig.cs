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

public sealed class DwarfSettings
{
    [JsonPropertyName("MinimumScale")]
    public float MinimumScale { get; set; } = 0.60f;

    [JsonPropertyName("MaximumScale")]
    public float MaximumScale { get; set; } = 0.95f;
}

public sealed class EnemySpinSettings
{
    [JsonPropertyName("MinimumChance")]
    public float MinimumChance { get; set; } = 0.20f;

    [JsonPropertyName("MaximumChance")]
    public float MaximumChance { get; set; } = 0.40f;
}

public sealed class DashSettings
{
    [JsonPropertyName("JumpVelocity")]
    public float JumpVelocity { get; set; } = 150.0f;

    [JsonPropertyName("PushVelocity")]
    public float PushVelocity { get; set; } = 600.0f;

    [JsonPropertyName("AnyDirection")]
    public bool AnyDirection { get; set; } = true;

    [JsonPropertyName("CooldownSeconds")]
    public float CooldownSeconds { get; set; } = 2.0f;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 1.0f;
}

public sealed class FriendlyFireSettings
{
    [JsonPropertyName("HealthDamageMultiplier")]
    public float HealthDamageMultiplier { get; set; } = 0.30f;
}

public sealed class FrozenDecoySettings
{
    [JsonPropertyName("TriggerRadius")]
    public float TriggerRadius { get; set; } = 180.0f;

    [JsonPropertyName("SlownessExponent")]
    public int SlownessExponent { get; set; } = 5;

    [JsonPropertyName("GrenadeLimit")]
    public int GrenadeLimit { get; set; } = 3;
}

public sealed class BladeMasterSettings
{
    [JsonPropertyName("TorsoDeflectionChance")]
    public float TorsoDeflectionChance { get; set; } = 0.95f;

    [JsonPropertyName("LegDeflectionChance")]
    public float LegDeflectionChance { get; set; } = 0.70f;
}

public sealed class MagneticDecoySettings
{
    [JsonPropertyName("TriggerRadius")]
    public float TriggerRadius { get; set; } = 180.0f;

    [JsonPropertyName("Strength")]
    public float Strength { get; set; } = 30.0f;

    [JsonPropertyName("GrenadeLimit")]
    public int GrenadeLimit { get; set; } = 3;
}

public sealed class DarknessSettings
{
    [JsonPropertyName("R")]
    public int Red { get; set; } = 0;

    [JsonPropertyName("G")]
    public int Green { get; set; } = 0;

    [JsonPropertyName("B")]
    public int Blue { get; set; } = 0;

    [JsonPropertyName("A")]
    public int Alpha { get; set; } = 230;
}

public sealed class HomingNadesSettings
{
    [JsonPropertyName("Strength")]
    public float Strength { get; set; } = 150.0f;

    [JsonPropertyName("MaximumVelocity")]
    public float MaximumVelocity { get; set; } = 2000.0f;

    [JsonPropertyName("DetonationRange")]
    public float DetonationRange { get; set; } = 130.0f;

    [JsonPropertyName("HeGrenadeCount")]
    public int HeGrenadeCount { get; set; } = 2;

    [JsonPropertyName("FlashbangCount")]
    public int FlashbangCount { get; set; } = 2;
}

public sealed class SpectatorSettings
{
    [JsonPropertyName("Distance")]
    public float Distance { get; set; } = 100.0f;
}

public sealed class CypherSettings
{
    [JsonPropertyName("DeployCooldownSeconds")]
    public float DeployCooldownSeconds { get; set; } = 30.0f;

    [JsonPropertyName("MaximumDistance")]
    public float MaximumDistance { get; set; } = 4096.0f;

    [JsonPropertyName("SurfaceOffset")]
    public float SurfaceOffset { get; set; } = 8.0f;

    [JsonPropertyName("ViewOffset")]
    public float ViewOffset { get; set; } = 25.0f;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 1.0f;
}

public sealed class GhoulSettings
{
    [JsonPropertyName("MaximumSkills")]
    public int MaximumSkills { get; set; } = 5;
}

public sealed class MindHackSettings
{
    [JsonPropertyName("DurationSeconds")]
    public float DurationSeconds { get; set; } = 15.0f;
}

public sealed class BlastShotSettings
{
    [JsonPropertyName("ExplosionRadius")]
    public float ExplosionRadius { get; set; } = 400.0f;

    [JsonPropertyName("ExplosionDamage")]
    public float ExplosionDamage { get; set; } = 60.0f;

    [JsonPropertyName("TeammateDamageMultiplier")]
    public float TeammateDamageMultiplier { get; set; } = 0.50f;

    [JsonPropertyName("CooldownSeconds")]
    public float CooldownSeconds { get; set; } = 10.0f;

    [JsonPropertyName("Force")]
    public float Force { get; set; } = 1000.0f;
}

public sealed class FlashlightSettings
{
    [JsonPropertyName("ColorR")]
    public int ColorR { get; set; } = 255;

    [JsonPropertyName("ColorG")]
    public int ColorG { get; set; } = 255;

    [JsonPropertyName("ColorB")]
    public int ColorB { get; set; } = 255;

    [JsonPropertyName("Brightness")]
    public float Brightness { get; set; } = 1.5f;

    [JsonPropertyName("Range")]
    public float Range { get; set; } = 1200.0f;

    [JsonPropertyName("BlindDuration")]
    public float BlindDuration { get; set; } = 5.0f;

    [JsonPropertyName("BlindAlpha")]
    public float BlindAlpha { get; set; } = 200.0f;

    [JsonPropertyName("BlindAngle")]
    public float BlindAngle { get; set; } = 10.0f;
}

public sealed class FortniteSettings
{
    [JsonPropertyName("BarricadeHealth")]
    public int BarricadeHealth { get; set; } = 115;

    [JsonPropertyName("PlacementDistance")]
    public float PlacementDistance { get; set; } = 50.0f;

    [JsonPropertyName("PropModel")]
    public string PropModel { get; set; } = "models/props/de_aztec/hr_aztec/aztec_scaffolding/aztec_scaffold_wall_support_128.vmdl";

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 1.0f;
}

public sealed class DeadlyGrenadesSettings
{
    [JsonPropertyName("DamageMultiplier")]
    public float DamageMultiplier { get; set; } = 3.0f;

    [JsonPropertyName("RadiusMultiplier")]
    public float RadiusMultiplier { get; set; } = 5.0f;

    [JsonPropertyName("StartingGrenadeCount")]
    public int StartingGrenadeCount { get; set; } = 3;
}

public sealed class GrappleSettings
{
    [JsonPropertyName("MaximumDistance")]
    public float MaximumDistance { get; set; } = 1500.0f;

    [JsonPropertyName("MinimumDistance")]
    public float MinimumDistance { get; set; } = 150.0f;

    [JsonPropertyName("StopDistance")]
    public float StopDistance { get; set; } = 90.0f;

    [JsonPropertyName("PullSpeed")]
    public float PullSpeed { get; set; } = 850.0f;

    [JsonPropertyName("MaximumPullSeconds")]
    public float MaximumPullSeconds { get; set; } = 3.0f;

    [JsonPropertyName("RopeWidth")]
    public float RopeWidth { get; set; } = 0.8f;

    [JsonPropertyName("HookEmbed")]
    public float HookEmbed { get; set; } = 8.0f;

    [JsonPropertyName("HookScale")]
    public float HookScale { get; set; } = 0.4f;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 1.0f;
}

public sealed class JumpCurseSettings
{
    [JsonPropertyName("JumpVelocity")]
    public float JumpVelocity { get; set; } = 301.0f;
}

public sealed class PusherSettings
{
    [JsonPropertyName("MinimumChance")]
    public float MinimumChance { get; set; } = 0.30f;

    [JsonPropertyName("MaximumChance")]
    public float MaximumChance { get; set; } = 0.40f;

    [JsonPropertyName("JumpVelocity")]
    public float JumpVelocity { get; set; } = 300.0f;

    [JsonPropertyName("PushVelocity")]
    public float PushVelocity { get; set; } = 400.0f;
}

public sealed class ThrowingKnifeSettings
{
    [JsonPropertyName("ThrowForce")]
    public float ThrowForce { get; set; } = 2000.0f;

    [JsonPropertyName("TriggerRadius")]
    public float TriggerRadius { get; set; } = 10.0f;

    [JsonPropertyName("Damage")]
    public float Damage { get; set; } = 9999.0f;

    [JsonPropertyName("FriendlyFire")]
    public bool FriendlyFire { get; set; } = false;
}

public sealed class SmallButDeadlySettings
{
    [JsonPropertyName("PlayerScale")]
    public float PlayerScale { get; set; } = 0.50f;

    [JsonPropertyName("SpeedMultiplier")]
    public float SpeedMultiplier { get; set; } = 2.0f;

    [JsonPropertyName("Health")]
    public int Health { get; set; } = 10;
}

public sealed class JumperSettings
{
    [JsonPropertyName("JumpVelocity")]
    public float JumpVelocity { get; set; } = 300.0f;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 1.0f;
}

public sealed class DecoyXRaySettings
{
    [JsonPropertyName("GrenadeCount")]
    public int GrenadeCount { get; set; } = 3;

    [JsonPropertyName("RevealRadius")]
    public float RevealRadius { get; set; } = 500.0f;

    [JsonPropertyName("RevealDurationSeconds")]
    public float RevealDurationSeconds { get; set; } = 10.0f;
}

public sealed class RangeFinderSettings
{
    [JsonPropertyName("XrayDistanceThreshold")]
    public float XrayDistanceThreshold { get; set; } = 500.0f;

    [JsonPropertyName("UnitsPerMeter")]
    public float UnitsPerMeter { get; set; } = 100.0f;

    [JsonPropertyName("UpdateIntervalSeconds")]
    public float UpdateIntervalSeconds { get; set; } = 0.15f;
}

public sealed class ExplodingBarrelSettings
{
    [JsonPropertyName("ExplosionDamage")]
    public float ExplosionDamage { get; set; } = 50.0f;

    [JsonPropertyName("ExplosionRadius")]
    public float ExplosionRadius { get; set; } = 600.0f;

    [JsonPropertyName("PlacementDistance")]
    public float PlacementDistance { get; set; } = 50.0f;

    [JsonPropertyName("PropModel")]
    public string PropModel { get; set; } = "models/props/de_train/hr_t/barrel_a/barrel_a.vmdl";
}

public sealed class RamboSettings
{
    [JsonPropertyName("MinimumExtraHealth")]
    public int MinimumExtraHealth { get; set; } = 50;

    [JsonPropertyName("MaximumExtraHealthExclusive")]
    public int MaximumExtraHealthExclusive { get; set; } = 501;
}

public sealed class ToxicSmokeSettings
{
    [JsonPropertyName("Damage")]
    public int Damage { get; set; } = 2;

    [JsonPropertyName("Radius")]
    public float Radius { get; set; } = 180.0f;

    [JsonPropertyName("TickInterval")]
    public int TickInterval { get; set; } = 17;

    [JsonPropertyName("GrenadeLimit")]
    public int GrenadeLimit { get; set; } = 1;

    [JsonPropertyName("TeammateDamageMultiplier")]
    public float TeammateDamageMultiplier { get; set; } = 0.50f;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 0.30f;
}

public sealed class HealingSmokeSettings
{
    [JsonPropertyName("HealPerTick")]
    public int HealPerTick { get; set; } = 1;

    [JsonPropertyName("Radius")]
    public float Radius { get; set; } = 180.0f;

    [JsonPropertyName("TickInterval")]
    public int TickInterval { get; set; } = 16;

    [JsonPropertyName("MaximumHealth")]
    public int MaximumHealth { get; set; } = 150;

    [JsonPropertyName("Replenishments")]
    public int Replenishments { get; set; } = 1;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 0.50f;
}

public sealed class PyroSettings
{
    [JsonPropertyName("RegenerationMultiplier")]
    public float RegenerationMultiplier { get; set; } = 1.5f;

    [JsonPropertyName("GrenadeLimit")]
    public int GrenadeLimit { get; set; } = 2;
}

public sealed class RichBoySettings
{
    [JsonPropertyName("MinimumMoney")]
    public int MinimumMoney { get; set; } = 5000;

    [JsonPropertyName("MaximumMoney")]
    public int MaximumMoney { get; set; } = 15000;
}

public sealed class ThornsSettings
{
    [JsonPropertyName("DamageScale")]
    public float DamageScale { get; set; } = 0.30f;

    [JsonPropertyName("MaximumDamagePerHit")]
    public int MaximumDamagePerHit { get; set; } = 37;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 0.35f;
}

public sealed class NinjaSettings
{
    [JsonPropertyName("IdleInvisibility")]
    public float IdleInvisibility { get; set; } = 0.33f;

    [JsonPropertyName("CrouchInvisibility")]
    public float CrouchInvisibility { get; set; } = 0.33f;

    [JsonPropertyName("KnifeInvisibility")]
    public float KnifeInvisibility { get; set; } = 0.33f;
}

public sealed class PilotSettings
{
    [JsonPropertyName("MaximumFuel")]
    public float MaximumFuel { get; set; } = 150.0f;

    [JsonPropertyName("FuelConsumption")]
    public float FuelConsumption { get; set; } = 0.64f;

    [JsonPropertyName("Refuelling")]
    public float Refuelling { get; set; } = 0.10f;

    [JsonPropertyName("ForwardAcceleration")]
    public float ForwardAcceleration { get; set; } = 5.0f;

    [JsonPropertyName("UpwardAcceleration")]
    public float UpwardAcceleration { get; set; } = 12.0f;
}

public sealed class BombMinerSettings
{
    [JsonPropertyName("DetonationRange")]
    public float DetonationRange { get; set; } = 130.0f;

    [JsonPropertyName("ArmingSeconds")]
    public float ArmingSeconds { get; set; } = 3.0f;

    [JsonPropertyName("DetonationDelaySeconds")]
    public float DetonationDelaySeconds { get; set; } = 0.50f;

    [JsonPropertyName("DamageMultiplier")]
    public float DamageMultiplier { get; set; } = 2.0f;

    [JsonPropertyName("RadiusMultiplier")]
    public float RadiusMultiplier { get; set; } = 2.0f;

    [JsonPropertyName("GrenadeLimit")]
    public int GrenadeLimit { get; set; } = 3;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 1.0f;
}

public sealed class SoundMakerSettings
{
    [JsonPropertyName("CooldownSeconds")]
    public float CooldownSeconds { get; set; } = 2.0f;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 1.0f;
}

public sealed class ThirdEyeSettings
{
    [JsonPropertyName("Distance")]
    public float Distance { get; set; } = 100.0f;
}

public sealed class FalconEyeSettings
{
    [JsonPropertyName("Distance")]
    public float Distance { get; set; } = 1000.0f;
}

public sealed class TimeRecallSettings
{
    [JsonPropertyName("HistorySeconds")]
    public float HistorySeconds { get; set; } = 5.0f;

    [JsonPropertyName("CaptureIntervalSeconds")]
    public float CaptureIntervalSeconds { get; set; } = 0.25f;
}

public sealed class TimeControllerSettings
{
    [JsonPropertyName("SlowSpeed")]
    public float SlowSpeed { get; set; } = 0.75f;

    [JsonPropertyName("NormalSpeed")]
    public float NormalSpeed { get; set; } = 1.0f;

    [JsonPropertyName("FastSpeed")]
    public float FastSpeed { get; set; } = 1.5f;
}

public sealed class MuhammadSettings
{
    [JsonPropertyName("ExplosionDamage")]
    public float ExplosionDamage { get; set; } = 1500.0f;

    [JsonPropertyName("ExplosionRadius")]
    public float ExplosionRadius { get; set; } = 1000.0f;

    [JsonPropertyName("TeammateDamageMultiplier")]
    public float TeammateDamageMultiplier { get; set; } = 0.50f;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 1.0f;
}

public sealed class DisarmSettings
{
    [JsonPropertyName("MinimumChance")]
    public float MinimumChance { get; set; } = 0.20f;

    [JsonPropertyName("MaximumChance")]
    public float MaximumChance { get; set; } = 0.35f;
}

public sealed class KillerFlashSettings
{
    [JsonPropertyName("MinimumFlashDuration")]
    public float MinimumFlashDuration { get; set; } = 1.0f;

    [JsonPropertyName("FriendlyFire")]
    public bool FriendlyFire { get; set; } = true;

    [JsonPropertyName("LethalDamage")]
    public float LethalDamage { get; set; } = 9999.0f;
}

public sealed class PhoenixSettings
{
    [JsonPropertyName("MinimumChance")]
    public float MinimumChance { get; set; } = 0.20f;

    [JsonPropertyName("MaximumChance")]
    public float MaximumChance { get; set; } = 0.40f;

    [JsonPropertyName("ReviveHealth")]
    public int ReviveHealth { get; set; } = 100;
}

public sealed class SecondChanceSettings
{
    [JsonPropertyName("ReviveHealth")]
    public int ReviveHealth { get; set; } = 50;
}

public sealed class AntiFlashSettings
{
    [JsonPropertyName("FlashDuration")]
    public float FlashDuration { get; set; } = 7.0f;

    [JsonPropertyName("GrenadeCount")]
    public int GrenadeCount { get; set; } = 2;
}

public sealed class ChickenSettings
{
    [JsonPropertyName("SpeedMultiplier")]
    public float SpeedMultiplier { get; set; } = 1.10f;

    [JsonPropertyName("HealthPenalty")]
    public int HealthPenalty { get; set; } = 50;

    [JsonPropertyName("PlayerScale")]
    public float PlayerScale { get; set; } = 0.20f;
}

public sealed class HealingChickenSettings
{
    [JsonPropertyName("Amount")]
    public int Amount { get; set; } = 3;

    [JsonPropertyName("HealPerTick")]
    public int HealPerTick { get; set; } = 2;

    [JsonPropertyName("HealIntervalSeconds")]
    public float HealIntervalSeconds { get; set; } = 0.25f;

    [JsonPropertyName("HealRadius")]
    public float HealRadius { get; set; } = 150.0f;

    [JsonPropertyName("ChickenHealth")]
    public int ChickenHealth { get; set; } = 50;

    [JsonPropertyName("SpawnRadius")]
    public float SpawnRadius { get; set; } = 100.0f;

    [JsonPropertyName("MaximumHealth")]
    public int MaximumHealth { get; set; }
}

public sealed class FindThemSettings
{
    [JsonPropertyName("CooldownSeconds")]
    public float CooldownSeconds { get; set; } = 30.0f;

    [JsonPropertyName("ChickenHealth")]
    public int ChickenHealth { get; set; } = 30;

    [JsonPropertyName("SpawnRadius")]
    public float SpawnRadius { get; set; } = 48.0f;
}

public sealed class KamikazeChickenSettings
{
    [JsonPropertyName("CooldownSeconds")]
    public float CooldownSeconds { get; set; } = 30.0f;

    [JsonPropertyName("SpawnDistance")]
    public float SpawnDistance { get; set; } = 48.0f;

    [JsonPropertyName("ModelScale")]
    public float ModelScale { get; set; } = 1.35f;

    [JsonPropertyName("SpeedMultiplier")]
    public float SpeedMultiplier { get; set; } = 1.20f;

    [JsonPropertyName("MaximumSpeed")]
    public float MaximumSpeed { get; set; } = 180.0f;

    [JsonPropertyName("DetonationDistance")]
    public float DetonationDistance { get; set; } = 120.0f;

    [JsonPropertyName("ExplosionDamage")]
    public float ExplosionDamage { get; set; } = 100.0f;

    [JsonPropertyName("ExplosionRadius")]
    public float ExplosionRadius { get; set; } = 350.0f;

    [JsonPropertyName("TeammateDamageMultiplier")]
    public float TeammateDamageMultiplier { get; set; } = 0.50f;
}

public sealed class FlashJumpSettings
{
    [JsonPropertyName("BaseJumpVelocity")]
    public float BaseJumpVelocity { get; set; } = 200.0f;

    [JsonPropertyName("VelocityPerBlindSecond")]
    public float VelocityPerBlindSecond { get; set; } = 200.0f;

    [JsonPropertyName("MaximumJumpVelocity")]
    public float MaximumJumpVelocity { get; set; } = 800.0f;

    [JsonPropertyName("MaximumReplenishments")]
    public int MaximumReplenishments { get; set; } = 2;
}

public sealed class GlazSettings
{
    [JsonPropertyName("GrenadeCount")]
    public int GrenadeCount { get; set; } = 3;
}

public sealed class UnluckyCouplesSettings
{
    [JsonPropertyName("DamageMultiplier")]
    public float DamageMultiplier { get; set; } = 2.0f;
}

public sealed class SuperKnockbackSettings
{
    [JsonPropertyName("KnockbackForce")]
    public float KnockbackForce { get; set; } = 1500.0f;

    [JsonPropertyName("UpwardForce")]
    public float UpwardForce { get; set; } = 200.0f;

    [JsonPropertyName("MaximumSpeed")]
    public float MaximumSpeed { get; set; } = 1000.0f;
}

public sealed class SuperRecoilSettings
{
    [JsonPropertyName("RecoilForce")]
    public float RecoilForce { get; set; } = 500.0f;

    [JsonPropertyName("UpwardRatio")]
    public float UpwardRatio { get; set; } = 0.30f;

    [JsonPropertyName("MaximumSpeed")]
    public float MaximumSpeed { get; set; } = 600.0f;
}

public sealed class HolyHandGrenadeSettings
{
    [JsonPropertyName("DamageMultiplier")]
    public float DamageMultiplier { get; set; } = 2.5f;

    [JsonPropertyName("RadiusMultiplier")]
    public float RadiusMultiplier { get; set; } = 2.5f;

    [JsonPropertyName("MaximumReplenishments")]
    public int MaximumReplenishments { get; set; } = 1;
}

public sealed class KillInvincibilitySettings
{
    [JsonPropertyName("DurationSeconds")]
    public float DurationSeconds { get; set; } = 5.0f;
}

public sealed class InaccurateSettings
{
    [JsonPropertyName("ForcedSpread")]
    public float ForcedSpread { get; set; } = 0.088f;
}

public sealed class GodModeSettings
{
    [JsonPropertyName("DurationSeconds")]
    public float DurationSeconds { get; set; } = 2.0f;
}

public sealed class IllusionistSettings
{
    [JsonPropertyName("RunDurationSeconds")]
    public float RunDurationSeconds { get; set; } = 5.0f;

    [JsonPropertyName("CrouchDurationSeconds")]
    public float CrouchDurationSeconds { get; set; } = 12.0f;

    [JsonPropertyName("RunSpeed")]
    public float RunSpeed { get; set; } = 224.0f;

    [JsonPropertyName("CrouchSpeed")]
    public float CrouchSpeed { get; set; } = 80.0f;

    [JsonPropertyName("EnemyDamage")]
    public float EnemyDamage { get; set; } = 20.0f;

    [JsonPropertyName("SpawnDistance")]
    public float SpawnDistance { get; set; } = 40.0f;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 1.0f;
}

public sealed class LongKnifeSettings
{
    [JsonPropertyName("MaximumDistance")]
    public float MaximumDistance { get; set; } = 4096.0f;

    [JsonPropertyName("FriendlyFire")]
    public bool FriendlyFire { get; set; }

    [JsonPropertyName("Damage")]
    public float Damage { get; set; } = 9999.0f;
}

public sealed class LongZeusSettings
{
    [JsonPropertyName("MaximumDistance")]
    public float MaximumDistance { get; set; } = 4096.0f;

    [JsonPropertyName("FriendlyFire")]
    public bool FriendlyFire { get; set; }

    [JsonPropertyName("Damage")]
    public float Damage { get; set; } = 9999.0f;
}

public sealed class HotBombSettings
{
    [JsonPropertyName("DamageIntervalSeconds")]
    public float DamageIntervalSeconds { get; set; } = 1.0f;

    [JsonPropertyName("Damage")]
    public float Damage { get; set; } = 2.0f;

    [JsonPropertyName("SoundVolume")]
    public float SoundVolume { get; set; } = 0.35f;
}

public sealed class MagnifierSettings
{
    [JsonPropertyName("CustomFov")]
    public uint CustomFov { get; set; } = 50;
}

public sealed class TrackerSettings
{
    [JsonPropertyName("ParticleName")]
    public string ParticleName { get; set; } = "particles/ui/hud/ui_map_def_utility_trail.vpcf";
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
        ["SpeedBoost"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["DeathNote"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["ZoneReaper"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = 1
        },
        ["Ghoul"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["MindHack"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Rare",
            MaxPerServer = -1
        },
        ["Duplicator"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Deactivator"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Epic",
            MaxPerServer = -1
        },
        ["ChooseOneOfThree"] = new()
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
        ["IronHead"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Dwarf"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["EnemySpin"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["FireRain"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Epic",
            MaxPerServer = -1
        },
        ["Dash"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["FriendlyFire"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["FrozenDecoy"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["BladeMaster"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["MagneticDecoy"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["DecoyXRay"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["RangeFinder"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["InfiniteAmmo"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["ExplodingBarrel"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = 2
        },
        ["EnemySpawn"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["OneShot"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["NoRecoil"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Prosthesis"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["QuickShot"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Rambo"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["RadarHack"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["ToxicSmoke"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["HealingSmoke"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Pyro"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["RichBoy"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Thorns"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Grenadier"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Ninja"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Pilot"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Meito"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Rare",
            MaxPerServer = -1
        },
        ["BombMiner"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["SoundMaker"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Silent"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["ThirdEye"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["FalconEye"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Cypher"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["TimeRecall"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["TimeController"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Muhammad"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Disarm"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["KillerFlash"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Epic",
            MaxPerServer = 1
        },
        ["Phoenix"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["SecondChance"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Ghost"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Epic",
            MaxPerServer = -1
        },
        ["AntiFlash"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Chicken"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["HealingChicken"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Legendary",
            MaxPerServer = 1
        },
        ["FindThem"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Rare",
            MaxPerServer = 1
        },
        ["KamikazeChicken"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Rare",
            MaxPerServer = 1
        },
        ["FlashJump"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Glaz"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["HolyHandGrenade"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["KillInvincibility"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["GodMode"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Illusionist"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = 2
        },
        ["LongKnife"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["LongZeus"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Uncommon",
            MaxPerServer = -1
        },
        ["HotBomb"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = 1
        },
        ["Magnifier"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Tracker"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = 1
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
        ["Darkness"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Rare",
            MaxPerServer = -1
        },
        ["HomingNades"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Spectator"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["BlastShot"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Flashlight"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Legendary",
            MaxPerServer = 2
        },
        ["Fortnite"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = 5
        },
        ["Grapple"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Rare",
            MaxPerServer = -1
        },
        ["JumpCurse"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Pusher"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["ThrowingKnife"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = 1
        },
        ["Jumper"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Jammer"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
            MaxPerServer = -1
        },
        ["Deaf"] = new()
        {
            Enabled = true,
            Weight = 10,
            Rarity = "Common",
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

    [JsonPropertyName("BladeMaster")]
    public BladeMasterSettings BladeMaster { get; set; } = new();

    [JsonPropertyName("Dwarf")]
    public DwarfSettings Dwarf { get; set; } = new();

    [JsonPropertyName("EnemySpin")]
    public EnemySpinSettings EnemySpin { get; set; } = new();

    [JsonPropertyName("Dash")]
    public DashSettings Dash { get; set; } = new();

    [JsonPropertyName("FriendlyFire")]
    public FriendlyFireSettings FriendlyFire { get; set; } = new();

    [JsonPropertyName("FrozenDecoy")]
    public FrozenDecoySettings FrozenDecoy { get; set; } = new();

    [JsonPropertyName("MagneticDecoy")]
    public MagneticDecoySettings MagneticDecoy { get; set; } = new();

    [JsonPropertyName("DecoyXRay")]
    public DecoyXRaySettings DecoyXRay { get; set; } = new();

    [JsonPropertyName("RangeFinder")]
    public RangeFinderSettings RangeFinder { get; set; } = new();

    [JsonPropertyName("ExplodingBarrel")]
    public ExplodingBarrelSettings ExplodingBarrel { get; set; } = new();

    [JsonPropertyName("Rambo")]
    public RamboSettings Rambo { get; set; } = new();

    [JsonPropertyName("ToxicSmoke")]
    public ToxicSmokeSettings ToxicSmoke { get; set; } = new();

    [JsonPropertyName("HealingSmoke")]
    public HealingSmokeSettings HealingSmoke { get; set; } = new();

    [JsonPropertyName("Pyro")]
    public PyroSettings Pyro { get; set; } = new();

    [JsonPropertyName("RichBoy")]
    public RichBoySettings RichBoy { get; set; } = new();

    [JsonPropertyName("Thorns")]
    public ThornsSettings Thorns { get; set; } = new();

    [JsonPropertyName("Ninja")]
    public NinjaSettings Ninja { get; set; } = new();

    [JsonPropertyName("Pilot")]
    public PilotSettings Pilot { get; set; } = new();

    [JsonPropertyName("BombMiner")]
    public BombMinerSettings BombMiner { get; set; } = new();

    [JsonPropertyName("SoundMaker")]
    public SoundMakerSettings SoundMaker { get; set; } = new();

    [JsonPropertyName("ThirdEye")]
    public ThirdEyeSettings ThirdEye { get; set; } = new();

    [JsonPropertyName("FalconEye")]
    public FalconEyeSettings FalconEye { get; set; } = new();

    [JsonPropertyName("Cypher")]
    public CypherSettings Cypher { get; set; } = new();

    [JsonPropertyName("Ghoul")]
    public GhoulSettings Ghoul { get; set; } = new();

    [JsonPropertyName("MindHack")]
    public MindHackSettings MindHack { get; set; } = new();

    [JsonPropertyName("TimeRecall")]
    public TimeRecallSettings TimeRecall { get; set; } = new();

    [JsonPropertyName("TimeController")]
    public TimeControllerSettings TimeController { get; set; } = new();

    [JsonPropertyName("Muhammad")]
    public MuhammadSettings Muhammad { get; set; } = new();

    [JsonPropertyName("Disarm")]
    public DisarmSettings Disarm { get; set; } = new();

    [JsonPropertyName("KillerFlash")]
    public KillerFlashSettings KillerFlash { get; set; } = new();

    [JsonPropertyName("Phoenix")]
    public PhoenixSettings Phoenix { get; set; } = new();

    [JsonPropertyName("SecondChance")]
    public SecondChanceSettings SecondChance { get; set; } = new();

    [JsonPropertyName("AntiFlash")]
    public AntiFlashSettings AntiFlash { get; set; } = new();

    [JsonPropertyName("Chicken")]
    public ChickenSettings Chicken { get; set; } = new();

    [JsonPropertyName("HealingChicken")]
    public HealingChickenSettings HealingChicken { get; set; } = new();

    [JsonPropertyName("FindThem")]
    public FindThemSettings FindThem { get; set; } = new();

    [JsonPropertyName("KamikazeChicken")]
    public KamikazeChickenSettings KamikazeChicken { get; set; } = new();

    [JsonPropertyName("FlashJump")]
    public FlashJumpSettings FlashJump { get; set; } = new();

    [JsonPropertyName("Glaz")]
    public GlazSettings Glaz { get; set; } = new();

    [JsonPropertyName("HolyHandGrenade")]
    public HolyHandGrenadeSettings HolyHandGrenade { get; set; } = new();

    [JsonPropertyName("KillInvincibility")]
    public KillInvincibilitySettings KillInvincibility { get; set; } = new();

    [JsonPropertyName("GodMode")]
    public GodModeSettings GodMode { get; set; } = new();

    [JsonPropertyName("Illusionist")]
    public IllusionistSettings Illusionist { get; set; } = new();

    [JsonPropertyName("LongKnife")]
    public LongKnifeSettings LongKnife { get; set; } = new();

    [JsonPropertyName("LongZeus")]
    public LongZeusSettings LongZeus { get; set; } = new();

    [JsonPropertyName("HotBomb")]
    public HotBombSettings HotBomb { get; set; } = new();

    [JsonPropertyName("Magnifier")]
    public MagnifierSettings Magnifier { get; set; } = new();

    [JsonPropertyName("Tracker")]
    public TrackerSettings Tracker { get; set; } = new();

    [JsonPropertyName("UnluckyCouples")]
    public UnluckyCouplesSettings UnluckyCouples { get; set; } = new();

    [JsonPropertyName("SuperKnockback")]
    public SuperKnockbackSettings SuperKnockback { get; set; } = new();

    [JsonPropertyName("SuperRecoil")]
    public SuperRecoilSettings SuperRecoil { get; set; } = new();

    [JsonPropertyName("Inaccurate")]
    public InaccurateSettings Inaccurate { get; set; } = new();

    [JsonPropertyName("ExplosiveShot")]
    public ExplosiveShotSettings ExplosiveShot { get; set; } = new();

    [JsonPropertyName("Nightmare")]
    public NightmareSettings Nightmare { get; set; } = new();

    [JsonPropertyName("Darkness")]
    public DarknessSettings Darkness { get; set; } = new();

    [JsonPropertyName("HomingNades")]
    public HomingNadesSettings HomingNades { get; set; } = new();

    [JsonPropertyName("Spectator")]
    public SpectatorSettings Spectator { get; set; } = new();

    [JsonPropertyName("BlastShot")]
    public BlastShotSettings BlastShot { get; set; } = new();

    [JsonPropertyName("Flashlight")]
    public FlashlightSettings Flashlight { get; set; } = new();

    [JsonPropertyName("Fortnite")]
    public FortniteSettings Fortnite { get; set; } = new();

    [JsonPropertyName("DeadlyGrenades")]
    public DeadlyGrenadesSettings DeadlyGrenades { get; set; } = new();

    [JsonPropertyName("Grapple")]
    public GrappleSettings Grapple { get; set; } = new();

    [JsonPropertyName("JumpCurse")]
    public JumpCurseSettings JumpCurse { get; set; } = new();

    [JsonPropertyName("Pusher")]
    public PusherSettings Pusher { get; set; } = new();

    [JsonPropertyName("ThrowingKnife")]
    public ThrowingKnifeSettings ThrowingKnife { get; set; } = new();

    [JsonPropertyName("SmallButDeadly")]
    public SmallButDeadlySettings SmallButDeadly { get; set; } = new();

    [JsonPropertyName("Jumper")]
    public JumperSettings Jumper { get; set; } = new();

    [JsonPropertyName("EventsEnabled")]
    public bool EventsEnabled { get; set; } = true;

    [JsonPropertyName("MaxEventsPerRound")]
    public int MaxEventsPerRound { get; set; } = 4;

    [JsonPropertyName("EventRepeatBlockRounds")]
    public int EventRepeatBlockRounds { get; set; } = 4;

    [JsonPropertyName("ChooseCarnivalSkillId")]
    public string ChooseCarnivalSkillId { get; set; } = "ChooseOneOfThree";

    [JsonPropertyName("FastBunnyHop")]
    public FastBunnyHopSettings FastBunnyHop { get; set; } = new();

    [JsonPropertyName("Events")]
    public Dictionary<string, EventOverrideConfig> Events { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NormalRound"] = new() { Enabled = true, Weight = 10 },
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
        ["Bankruptcy"] = new() { Enabled = true, Weight = 10 },
        ["InfiniteAmmoMode"] = new() { Enabled = true, Weight = 10 },
        ["DeadlyGrenades"] = new() { Enabled = true, Weight = 10 },
        ["SmallButDeadly"] = new() { Enabled = true, Weight = 10 },
        ["InfiniteColoredSmoke"] = new() { Enabled = true, Weight = 10 },
        ["UnluckyCouples"] = new() { Enabled = true, Weight = 10 },
        ["SuperKnockback"] = new() { Enabled = true, Weight = 10 },
        ["SuperRecoil"] = new() { Enabled = true, Weight = 10 },
        ["Inaccurate"] = new() { Enabled = true, Weight = 10 },
        ["SilentWorld"] = new() { Enabled = true, Weight = 10 },
        ["AnywhereBombPlant"] = new() { Enabled = true, Weight = 10 },
        ["KillerSatellite"] = new() { Enabled = true, Weight = 10 },
        ["SkillMaster"] = new() { Enabled = true, Weight = 10 },
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
