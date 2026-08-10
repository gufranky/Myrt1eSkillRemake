using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ToxicSmokeSkill : ISkill,
    ITickSkill,
    ISmokeDetonateSkill,
    ISmokeExpiredSkill,
    IGrenadeThrownSkill
{
    private sealed class ToxicSmokeState
    {
        public required int GrenadesRemaining { get; set; }
        public List<Vector> Smokes { get; } = new();
    }

    private readonly ToxicSmokeSettings _settings;

    public ToxicSmokeSkill(ToxicSmokeSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "ToxicSmoke",
        DisplayName = "有毒烟雾",
        Description = "你的烟雾弹会对范围内的玩家造成伤害。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "smoke-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new ToxicSmokeState
        {
            GrenadesRemaining = Math.Clamp(_settings.GrenadeLimit, 1, 10)
        });
        GiveSmoke(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnSmokeDetonate(in SkillContext context, EventSmokegrenadeDetonate @event)
    {
        if (context.State.TryGet<ToxicSmokeState>(out var state))
        {
            state.Smokes.Add(new Vector(@event.X, @event.Y, @event.Z));
        }
    }

    public void OnSmokeExpired(in SkillContext context, EventSmokegrenadeExpired @event)
    {
        if (context.State.TryGet<ToxicSmokeState>(out var state))
        {
            state.Smokes.RemoveAll(smoke =>
                NearlyEquals(smoke.X, @event.X)
                && NearlyEquals(smoke.Y, @event.Y)
                && NearlyEquals(smoke.Z, @event.Z));
        }
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!string.Equals(@event.Weapon, "smokegrenade", StringComparison.OrdinalIgnoreCase)
            || !context.State.TryGet<ToxicSmokeState>(out var state)
            || state.GrenadesRemaining <= 0)
        {
            return;
        }

        state.GrenadesRemaining--;
        if (state.GrenadesRemaining > 0)
        {
            GiveSmoke(context.Player);
        }
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<ToxicSmokeState>(out var state)
            || state.Smokes.Count == 0
            || Server.TickCount % Math.Max(1, _settings.TickInterval) != 0)
        {
            return;
        }

        var radius = float.IsFinite(_settings.Radius) ? Math.Max(1.0f, _settings.Radius) : 180.0f;
        var baseDamage = Math.Max(0, _settings.Damage);
        var teammateMultiplier = float.IsFinite(_settings.TeammateDamageMultiplier)
            ? Math.Clamp(_settings.TeammateDamageMultiplier, 0.0f, 1.0f)
            : 0.50f;

        foreach (var smoke in state.Smokes.ToArray())
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid || !player.PawnIsAlive)
                {
                    continue;
                }

                var pawn = player.PlayerPawn.Value;
                var origin = pawn?.AbsOrigin;
                if (pawn is null || !pawn.IsValid || origin is null || Distance(smoke, origin) > radius)
                {
                    continue;
                }

                var damage = baseDamage;
                if (player.Slot != context.Player.Slot && player.Team == context.Player.Team)
                {
                    damage = (int)MathF.Round(baseDamage * teammateMultiplier);
                }

                if (damage <= 0)
                {
                    continue;
                }

                pawn.Health -= damage;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                pawn.EmitSound(
                    "Player.DamageBody.Victim",
                    volume: float.IsFinite(_settings.SoundVolume)
                        ? Math.Clamp(_settings.SoundVolume, 0.0f, 1.0f)
                        : 0.30f);

                if (pawn.Health <= 0)
                {
                    Server.NextFrame(() =>
                    {
                        if (pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                        {
                            pawn.CommitSuicide(false, true);
                        }
                    });
                }
            }
        }
    }

    private static void GiveSmoke(CCSPlayerController player)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return;
        }

        var alreadyHasSmoke = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_smokegrenade" }) == true;
        if (!alreadyHasSmoke)
        {
            player.GiveNamedItem("weapon_smokegrenade");
        }
    }

    private static float Distance(Vector first, Vector second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        var z = first.Z - second.Z;
        return MathF.Sqrt(x * x + y * y + z * z);
    }

    private static bool NearlyEquals(float first, float second) => Math.Abs(first - second) < 0.01f;
}
