using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class MagneticDecoySkill : ISkill,
    ITickSkill,
    IDecoyStartedSkill,
    IDecoyDetonateSkill,
    IGrenadeThrownSkill
{
    private sealed record MagneticZone(int EntityId, Vector Position);

    private sealed class MagneticDecoyState
    {
        public int GrenadesRemaining { get; set; }
        public bool Active { get; set; } = true;
        public List<MagneticZone> Zones { get; } = new();
    }

    private readonly MagneticDecoySettings _settings;

    public MagneticDecoySkill(MagneticDecoySettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "MagneticDecoy",
        DisplayName = "🧲 磁诱饵",
        Description = "你的诱饵弹会持续吸引附近的所有玩家靠近。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "decoy-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new MagneticDecoyState
        {
            GrenadesRemaining = Math.Clamp(_settings.GrenadeLimit, 1, 10)
        };
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
        if (!context.State.TryGet<MagneticDecoyState>(out var state) || !state.Active)
        {
            return;
        }

        state.Zones.RemoveAll(zone => zone.EntityId == @event.Entityid);
        state.Zones.Add(new MagneticZone(
            @event.Entityid,
            new Vector(@event.X, @event.Y, @event.Z)));
    }

    public void OnDecoyDetonate(in SkillContext context, EventDecoyDetonate @event)
    {
        if (context.State.TryGet<MagneticDecoyState>(out var state))
        {
            state.Zones.RemoveAll(zone => zone.EntityId == @event.Entityid);
        }
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!GrenadeReplenishment.Matches(@event.Weapon, "decoy")
            || !context.State.TryGet<MagneticDecoyState>(out var state)
            || !state.Active
            || state.GrenadesRemaining <= 0)
        {
            return;
        }

        state.GrenadesRemaining--;
        if (state.GrenadesRemaining <= 0)
        {
            return;
        }

        var player = context.Player;
        context.Effects.AddTimer(GrenadeReplenishment.DelaySeconds, () =>
        {
            if (state.Active)
            {
                GiveDecoy(player);
            }
        });
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<MagneticDecoyState>(out var state)
            || !state.Active
            || state.Zones.Count == 0)
        {
            return;
        }

        var radius = PositiveFiniteOr(_settings.TriggerRadius, 180.0f);
        var baseStrength = NonNegativeFiniteOr(_settings.Strength, 30.0f);
        if (baseStrength <= 0.0f)
        {
            return;
        }

        foreach (var zone in state.Zones.ToArray())
        {
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
                if (pawn is not { IsValid: true } || origin is null)
                {
                    continue;
                }

                var deltaX = zone.Position.X - origin.X;
                var deltaY = zone.Position.Y - origin.Y;
                var deltaZ = zone.Position.Z - origin.Z;
                var distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
                var horizontalLength = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (distance > radius || distance <= 10.0f || horizontalLength <= 0.01f)
                {
                    continue;
                }

                var ratio = 1.0f - Math.Clamp(distance / radius, 0.0f, 1.0f);
                var strength = baseStrength * ratio;
                pawn.AbsVelocity.X += deltaX / horizontalLength * strength;
                pawn.AbsVelocity.Y += deltaY / horizontalLength * strength;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_vecAbsVelocity");
            }
        }
    }

    private static void GiveDecoy(CCSPlayerController player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
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

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static float NonNegativeFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value >= 0.0f ? value : fallback;
}
