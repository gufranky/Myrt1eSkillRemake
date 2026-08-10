using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Myrt1eSkill_Remake.Core;

public enum SkillKind
{
    Passive,
    Active
}

public enum SkillRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public sealed record SkillDescriptor
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public SkillKind Kind { get; init; } = SkillKind.Passive;
    public SkillRarity Rarity { get; init; } = SkillRarity.Common;
    public int DefaultWeight { get; init; } = 10;
    public int MaxPerServer { get; init; } = -1;
    public float CooldownSeconds { get; init; }
    public CsTeam OnlyTeam { get; init; } = CsTeam.None;
    public bool RequiresTeammate { get; init; }
    public string RequiredPermission { get; init; } = string.Empty;
    public IReadOnlySet<string> ConflictTags { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> IncompatibleEventIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public readonly record struct SkillContext(
    Myrt1eSkillRemakePlugin Plugin,
    CCSPlayerController Player,
    EffectScope Effects,
    SkillStateBag State);

public interface ISkill
{
    SkillDescriptor Descriptor { get; }

    void OnGranted(in SkillContext context);
    void OnActivated(in SkillContext context);
    void OnRevoked(in SkillContext context);
}

// Skills implement only the strongly typed events they actually consume.
public interface ITickSkill
{
    void OnTick(in SkillContext context);
}

public interface IPlayerHurtSkill
{
    void OnPlayerHurt(in SkillContext context, EventPlayerHurt @event);
}

public interface IPlayerHurtPreSkill
{
    void OnPlayerHurtPre(in SkillContext context, EventPlayerHurt @event);
}

public interface IPlayerDeathSkill
{
    void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event);
}

public interface IPlayerBlindSkill
{
    void OnPlayerBlind(in SkillContext context, EventPlayerBlind @event);
}

public interface IFlashbangDetonateSkill
{
    void OnFlashbangDetonate(in SkillContext context, EventFlashbangDetonate @event);
}

public interface IWeaponFireSkill
{
    void OnWeaponFire(in SkillContext context, EventWeaponFire @event);
}

public interface IBulletImpactSkill
{
    void OnBulletImpact(in SkillContext context, EventBulletImpact @event);
}

public interface IDecoyStartedSkill
{
    void OnDecoyStarted(in SkillContext context, EventDecoyStarted @event);
}

public interface IDecoyDetonateSkill
{
    void OnDecoyDetonate(in SkillContext context, EventDecoyDetonate @event);
}

public interface IGrenadeThrownSkill
{
    void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event);
}

public interface ISmokeDetonateSkill
{
    void OnSmokeDetonate(in SkillContext context, EventSmokegrenadeDetonate @event);
}

public interface ISmokeExpiredSkill
{
    void OnSmokeExpired(in SkillContext context, EventSmokegrenadeExpired @event);
}

public interface IPreDamageSkill
{
    void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo);
}

public interface IPreDamageAttackerSkill
{
    void OnBeforeDamageDealt(in SkillContext context, CCSPlayerController victim, CTakeDamageInfo damageInfo);
}
