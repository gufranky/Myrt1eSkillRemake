using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class FrozenDecoySkill : ISkill,
    ITickSkill,
    IDecoyStartedSkill,
    IDecoyDetonateSkill,
    IGrenadeThrownSkill
{
    private sealed record FrozenZone(int EntityId, Vector Position);

    private sealed class FrozenDecoyState
    {
        public required int GrenadesRemaining { get; set; }
        public bool Active { get; set; } = true;
        public List<FrozenZone> Zones { get; } = new();
    }

    private readonly FrozenDecoySettings _settings;

    public FrozenDecoySkill(FrozenDecoySettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "FrozenDecoy",
        DisplayName = "冷冻诱饵",
        Description = "你的诱饵弹会冻结附近的所有玩家。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "decoy-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var grenadeLimit = Math.Clamp(_settings.GrenadeLimit, 1, 10);
        var state = new FrozenDecoyState { GrenadesRemaining = grenadeLimit };
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => state.Active = false);
        GiveDecoy(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnDecoyStarted(in SkillContext context, EventDecoyStarted @event)
    {
        if (!context.State.TryGet<FrozenDecoyState>(out var state))
        {
            return;
        }

        state.Zones.RemoveAll(zone => zone.EntityId == @event.Entityid);
        state.Zones.Add(new FrozenZone(
            @event.Entityid,
            new Vector(@event.X, @event.Y, @event.Z)));
    }

    public void OnDecoyDetonate(in SkillContext context, EventDecoyDetonate @event)
    {
        if (context.State.TryGet<FrozenDecoyState>(out var state))
        {
            state.Zones.RemoveAll(zone => zone.EntityId == @event.Entityid);
        }
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!GrenadeReplenishment.Matches(@event.Weapon, "decoy")
            || !context.State.TryGet<FrozenDecoyState>(out var state)
            || !state.Active
            || state.GrenadesRemaining <= 0)
        {
            return;
        }

        state.GrenadesRemaining--;
        if (state.GrenadesRemaining > 0)
        {
            var player = context.Player;
            context.Effects.AddTimer(GrenadeReplenishment.DelaySeconds, () =>
            {
                if (state.Active)
                {
                    GiveDecoy(player);
                }
            });
        }
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<FrozenDecoyState>(out var state) || state.Zones.Count == 0)
        {
            return;
        }

        var configuredRadius = float.IsFinite(_settings.TriggerRadius)
            ? _settings.TriggerRadius
            : 180.0f;
        var radius = Math.Max(1.0f, configuredRadius);
        var exponent = Math.Clamp(_settings.SlownessExponent, 1, 20);

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid
                || !player.PawnIsAlive
                || player.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
            {
                continue;
            }

            var pawn = player.PlayerPawn.Value;
            var origin = pawn?.AbsOrigin;
            if (pawn is null || !pawn.IsValid || origin is null)
            {
                continue;
            }

            float? strongestModifier = null;
            foreach (var zone in state.Zones)
            {
                var deltaX = origin.X - zone.Position.X;
                var deltaY = origin.Y - zone.Position.Y;
                var deltaZ = origin.Z - zone.Position.Z;
                var distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
                if (distance > radius)
                {
                    continue;
                }

                var distanceRatio = Math.Clamp(distance / radius, 0.0f, 1.0f);
                var modifier = MathF.Pow(distanceRatio, exponent);
                strongestModifier = strongestModifier.HasValue
                    ? Math.Min(strongestModifier.Value, modifier)
                    : modifier;
            }

            if (strongestModifier.HasValue)
            {
                pawn.VelocityModifier = strongestModifier.Value;
            }
        }
    }

    private static void GiveDecoy(CCSPlayerController player)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return;
        }

        var alreadyHasDecoy = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_decoy" }) == true;
        if (!alreadyHasDecoy)
        {
            player.GiveNamedItem("weapon_decoy");
        }
    }
}
