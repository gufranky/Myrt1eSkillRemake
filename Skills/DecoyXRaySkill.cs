using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class DecoyXRaySkill : ISkill, IGrenadeThrownSkill, IDecoyDetonateSkill
{
    private sealed class DecoyXRayState
    {
        public required int GrenadesRemaining { get; set; }
        public int RevealSequence { get; set; }
        public bool Active { get; set; } = true;
    }

    private readonly DecoyXRaySettings _settings;
    private readonly WallhackService _wallhack;

    public DecoyXRaySkill(DecoyXRaySettings settings, WallhackService wallhack)
    {
        _settings = settings;
        _wallhack = wallhack;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "DecoyXRay",
        DisplayName = "💣 透视诱饵弹",
        Description = "开局3个诱饵弹，爆炸显示敌人位置！",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "decoy-behavior-control",
            "player-outline-vision",
            "radar-vision"
        },
        IncompatibleEventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SuperpowerXray",
            "Xray"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new DecoyXRayState
        {
            GrenadesRemaining = Math.Clamp(_settings.GrenadeCount, 1, 10)
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

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!GrenadeReplenishment.Matches(@event.Weapon, "decoy")
            || !context.State.TryGet<DecoyXRayState>(out var state)
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
                PluginText.Chat(player, $"[透视诱饵弹] 已补充诱饵弹，剩余 {state.GrenadesRemaining} 颗。");
            }
        });
    }

    public void OnDecoyDetonate(in SkillContext context, EventDecoyDetonate @event)
    {
        if (!context.State.TryGet<DecoyXRayState>(out var state) || !state.Active)
        {
            return;
        }

        var owner = context.Player;
        if (!owner.IsValid || owner.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
        {
            return;
        }

        var configuredRadius = float.IsFinite(_settings.RevealRadius) ? _settings.RevealRadius : 500.0f;
        var radius = Math.Max(1.0f, configuredRadius);
        var radiusSquared = radius * radius;
        var origin = new Vector(@event.X, @event.Y, @event.Z);
        var targets = Utilities.GetPlayers()
            .Where(target => IsEnemyInRange(owner, target, origin, radiusSquared))
            .ToArray();

        if (targets.Length == 0)
        {
            PluginText.Chat(owner, "[透视诱饵弹] 爆炸范围内没有发现敌人。");
            return;
        }

        var sourceId = $"skill:DecoyXRay:{owner.Index}:{++state.RevealSequence}";
        _wallhack.SetTargetedGrant(
            sourceId,
            targets.Select(target => (owner.Index, target.Index)));
        context.Effects.RegisterCleanup(() => _wallhack.RemoveGrant(sourceId));

        var duration = float.IsFinite(_settings.RevealDurationSeconds)
            ? Math.Max(0.1f, _settings.RevealDurationSeconds)
            : 10.0f;
        context.Effects.AddTimer(duration, () => _wallhack.RemoveGrant(sourceId));
        PluginText.Chat(owner, $"[透视诱饵弹] 已标记 {targets.Length} 名敌人，持续 {duration:F1} 秒。");
    }

    private static bool IsEnemyInRange(
        CCSPlayerController owner,
        CCSPlayerController target,
        Vector origin,
        float radiusSquared)
    {
        if (!target.IsValid
            || !target.PawnIsAlive
            || target.Team == owner.Team
            || target.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
        {
            return false;
        }

        var targetOrigin = target.PlayerPawn.Value?.AbsOrigin;
        if (targetOrigin is null)
        {
            return false;
        }

        var x = targetOrigin.X - origin.X;
        var y = targetOrigin.Y - origin.Y;
        var z = targetOrigin.Z - origin.Z;
        return (x * x) + (y * y) + (z * z) <= radiusSquared;
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
}
