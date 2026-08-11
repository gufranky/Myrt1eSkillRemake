using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class HealingSmokeSkill : ISkill,
    ITickSkill,
    ISmokeDetonateSkill,
    ISmokeExpiredSkill,
    IGrenadeThrownSkill,
    IEntitySpawnedSkill
{
    private sealed class HealingSmokeState
    {
        public int ReplenishmentsRemaining { get; set; }
        public bool Active { get; set; } = true;
        public List<Vector> Smokes { get; } = new();
    }

    private readonly HealingSmokeSettings _settings;

    public HealingSmokeSkill(HealingSmokeSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "HealingSmoke",
        DisplayName = "💚 治疗烟雾弹",
        Description = "开局获得绿色治疗烟雾弹，持续治疗队友至最高 150 生命；投掷后补充 1 次。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "smoke-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new HealingSmokeState
        {
            ReplenishmentsRemaining = Math.Clamp(_settings.Replenishments, 0, 10)
        };
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => state.Active = false);
        GiveSmoke(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnEntitySpawned(in SkillContext context, CEntityInstance entity)
    {
        if (!context.State.TryGet<HealingSmokeState>(out var state)
            || !state.Active
            || !string.Equals(entity.DesignerName, "smokegrenade_projectile", StringComparison.Ordinal))
        {
            return;
        }

        var smoke = entity.As<CSmokeGrenadeProjectile>();
        if (smoke is not { IsValid: true })
        {
            return;
        }

        smoke.SmokeColor.X = 0.0f;
        smoke.SmokeColor.Y = 255.0f;
        smoke.SmokeColor.Z = 0.0f;
        Utilities.SetStateChanged(smoke, "CSmokeGrenadeProjectile", "m_vSmokeColor");
    }

    public void OnSmokeDetonate(in SkillContext context, EventSmokegrenadeDetonate @event)
    {
        if (context.State.TryGet<HealingSmokeState>(out var state) && state.Active)
        {
            state.Smokes.Add(new Vector(@event.X, @event.Y, @event.Z));
        }
    }

    public void OnSmokeExpired(in SkillContext context, EventSmokegrenadeExpired @event)
    {
        if (context.State.TryGet<HealingSmokeState>(out var state))
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
            || !context.State.TryGet<HealingSmokeState>(out var state)
            || !state.Active
            || state.ReplenishmentsRemaining <= 0)
        {
            return;
        }

        state.ReplenishmentsRemaining--;
        var player = context.Player;
        context.Effects.AddTimer(0.01f, () =>
        {
            if (state.Active)
            {
                GiveSmoke(player);
                PluginText.Chat(player, "[治疗烟雾弹] 已补充烟雾弹（1/1）。");
            }
        });
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<HealingSmokeState>(out var state)
            || !state.Active
            || state.Smokes.Count == 0
            || Server.TickCount % Math.Max(1, _settings.TickInterval) != 0)
        {
            return;
        }

        var radius = PositiveFiniteOr(_settings.Radius, 180.0f);
        var heal = Math.Max(0, _settings.HealPerTick);
        var maximumHealth = Math.Max(1, _settings.MaximumHealth);
        if (heal <= 0)
        {
            return;
        }

        foreach (var smoke in state.Smokes.ToArray())
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid || !player.PawnIsAlive || player.Team != context.Player.Team)
                {
                    continue;
                }

                var pawn = player.PlayerPawn.Value;
                var origin = pawn?.AbsOrigin;
                if (pawn is not { IsValid: true }
                    || origin is null
                    || pawn.Health >= maximumHealth
                    || Distance(smoke, origin) > radius)
                {
                    continue;
                }

                pawn.Health = Math.Min(maximumHealth, pawn.Health + heal);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                pawn.EmitSound(
                    "Healthshot.Success",
                    volume: Math.Clamp(FiniteOr(_settings.SoundVolume, 0.50f), 0.0f, 1.0f));
            }
        }
    }

    private static void GiveSmoke(CCSPlayerController player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
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

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static float FiniteOr(float value, float fallback) =>
        float.IsFinite(value) ? value : fallback;
}
