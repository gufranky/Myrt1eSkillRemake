using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ExplosiveShotSkill : ISkill, IBulletImpactSkill
{
    private sealed class ExplosiveShotState
    {
        public required float Chance { get; init; }
        public int LastExplosionTick { get; set; } = -1;
    }

    private readonly ExplosiveShotSettings _settings;
    private readonly ExplosiveProjectileService _explosions;

    public ExplosiveShotSkill(
        ExplosiveShotSettings settings,
        ExplosiveProjectileService explosions)
    {
        _settings = settings;
        _explosions = explosions;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "ExplosiveShot",
        DisplayName = "爆炸子弹",
        Description = "射击时有随机概率让子弹落点发生爆炸。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bullet-impact-effect"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var configuredMinimum = float.IsFinite(_settings.MinimumChance) ? _settings.MinimumChance : 0.15f;
        var configuredMaximum = float.IsFinite(_settings.MaximumChance) ? _settings.MaximumChance : 0.30f;
        var minimum = Math.Clamp(Math.Min(configuredMinimum, configuredMaximum), 0.0f, 1.0f);
        var maximum = Math.Clamp(Math.Max(configuredMinimum, configuredMaximum), minimum, 1.0f);
        var chance = minimum + Random.Shared.NextSingle() * (maximum - minimum);

        context.State.Set(new ExplosiveShotState { Chance = chance });
        PluginText.Chat(context.Player, $"[随机技能] 爆炸子弹：触发概率 {chance:P0}");
    }

    public void OnBulletImpact(in SkillContext context, EventBulletImpact @event)
    {
        if (!context.Player.PawnIsAlive
            || !context.State.TryGet<ExplosiveShotState>(out var state)
            || state.LastExplosionTick == Server.TickCount
            || Random.Shared.NextSingle() > state.Chance)
        {
            return;
        }

        var impact = new Vector(@event.X, @event.Y, @event.Z);
        if (_explosions.TrySpawn(impact, context.Player))
        {
            state.LastExplosionTick = Server.TickCount;
        }
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
