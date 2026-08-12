using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Core;

public sealed record EventDescriptor
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public int DefaultWeight { get; init; } = 10;
    public bool CanBeNested { get; init; } = true;
    public int CompositeChildCount { get; init; }
    public IReadOnlySet<string> ExclusiveTags { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> IncompatibleEventIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> BlockedSkillTags { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public readonly record struct RoundEventContext(
    Myrt1eSkillRemakePlugin Plugin,
    RoundPlan Plan,
    EffectScope Effects);

public interface IRoundEvent
{
    EventDescriptor Descriptor { get; }

    void Contribute(RoundPlanBuilder builder);
    void OnApplied(in RoundEventContext context);
    void OnRemoved(in RoundEventContext context);
}

public interface IRoundEventTick
{
    void OnTick(in RoundEventContext context);
}

public interface IRoundEventPlayerHurt
{
    void OnPlayerHurt(in RoundEventContext context, EventPlayerHurt @event);
}

public interface IRoundEventWeaponFire
{
    void OnWeaponFire(in RoundEventContext context, EventWeaponFire @event);
}

public interface IRoundEventPlayerJump
{
    void OnPlayerJump(in RoundEventContext context, EventPlayerJump @event);
}

public interface IRoundEventWeaponReload
{
    void OnWeaponReload(in RoundEventContext context, EventWeaponReload @event);
}

public interface IRoundEventDecoyStarted
{
    void OnDecoyStarted(in RoundEventContext context, EventDecoyStarted @event);
}

public interface IRoundEventPreDamage
{
    void OnBeforeDamage(
        in RoundEventContext context,
        CCSPlayerController victim,
        CCSPlayerController attacker,
        CTakeDamageInfo damageInfo);
}

public interface IRoundEventGrenadeThrown
{
    void OnGrenadeThrown(in RoundEventContext context, EventGrenadeThrown @event);
}

public interface IRoundEventEntitySpawned
{
    void OnEntitySpawned(in RoundEventContext context, CEntityInstance entity);
}

public interface IRoundEventPlayerSpawn
{
    void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event);
}

public interface IRoundEventItemPickup
{
    void OnItemPickup(in RoundEventContext context, EventItemPickup @event);
}

public interface IRoundEventPlayerDisconnect
{
    void OnPlayerDisconnect(in RoundEventContext context, EventPlayerDisconnect @event);
}

public interface IRoundEventCheckTransmit
{
    void OnCheckTransmit(in RoundEventContext context, CCheckTransmitInfoList infoList);
}
