using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class MuhammadSkill : ISkill, IPlayerDeathSkill
{
    private static readonly string[] VoiceLines =
    [
        "balkan.radiobotfallback01",
        "balkan.radiobotfallback02",
        "balkan.radiobotfallback04"
    ];

    private readonly MuhammadSettings _settings;
    private readonly ExplosiveProjectileService _explosions;

    public MuhammadSkill(MuhammadSettings settings, ExplosiveProjectileService explosions)
    {
        _settings = settings;
        _explosions = explosions;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Muhammad",
        DisplayName = "💀 穆罕默德",
        Description = "你死后会立即爆炸，杀死附近的玩家。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "death-explosion"
        }
    };

    public void OnGranted(in SkillContext context)
    {
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        var victim = @event.Userid;
        if (victim is null || victim.Index != context.Player.Index)
        {
            return;
        }

        var pawn = victim.PlayerPawn.Value;
        if (pawn is not { IsValid: true } || pawn.AbsOrigin is null)
        {
            return;
        }

        var position = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + 10.0f);
        var ownerIndex = victim.Index;
        var team = victim.TeamNum;
        var damage = float.IsFinite(_settings.ExplosionDamage)
            ? Math.Max(0.0f, _settings.ExplosionDamage)
            : 1500.0f;
        var radius = float.IsFinite(_settings.ExplosionRadius)
            ? Math.Max(0.0f, _settings.ExplosionRadius)
            : 1000.0f;
        var teammateMultiplier = float.IsFinite(_settings.TeammateDamageMultiplier)
            ? Math.Clamp(_settings.TeammateDamageMultiplier, 0.0f, 1.0f)
            : 0.50f;
        var soundVolume = float.IsFinite(_settings.SoundVolume)
            ? Math.Clamp(_settings.SoundVolume, 0.0f, 1.0f)
            : 1.0f;

        pawn.EmitSound(VoiceLines[Random.Shared.Next(VoiceLines.Length)], volume: soundVolume);
        PluginText.ChatAll($"💀 {victim.PlayerName}：砰！");

        Server.NextWorldUpdate(() =>
        {
            if (!_explosions.TrySpawn(
                    position,
                    ownerIndex,
                    team,
                    damage,
                    radius,
                    teammateMultiplier))
            {
                PluginText.ChatAll("[穆罕默德] 爆炸生成失败。");
            }
        });
    }
}
