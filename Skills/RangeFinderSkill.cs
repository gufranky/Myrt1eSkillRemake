using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class RangeFinderSkill : ISkill, ITickSkill
{
    private const string StatusSource = "skill:RangeFinder";

    private sealed class RangeFinderState
    {
        public DateTime NextUpdateAt { get; set; }
        public uint? RevealedTarget { get; set; }
        public bool StatusVisible { get; set; }
        public bool Active { get; set; } = true;
    }

    private readonly RangeFinderSettings _settings;
    private readonly WallhackService _wallhack;

    public RangeFinderSkill(RangeFinderSettings settings, WallhackService wallhack)
    {
        _settings = settings;
        _wallhack = wallhack;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "RangeFinder",
        DisplayName = "📏 测距仪",
        Description = "显示到最近敌人的距离！5米内敌人会被透视标记！",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "nearest-enemy-ranging"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new RangeFinderState();
        context.State.Set(state);
        var player = context.Player;
        var grantId = GrantId(player);
        var presentation = context.Plugin.RuntimePresentation;
        context.Effects.RegisterCleanup(() =>
        {
            state.Active = false;
            _wallhack.RemoveGrant(grantId);
            presentation.RemoveStatusLine(player, StatusSource);
        });
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<RangeFinderState>(out var state) || !state.Active)
        {
            return;
        }

        var player = context.Player;
        if (!player.IsValid || !player.PawnIsAlive)
        {
            ClearReveal(player, state);
            if (state.StatusVisible)
            {
                context.Plugin.RuntimePresentation.RemoveStatusLine(player, StatusSource);
                state.StatusVisible = false;
            }

            return;
        }

        var now = DateTime.UtcNow;
        if (now < state.NextUpdateAt)
        {
            return;
        }

        state.NextUpdateAt = now.AddSeconds(UpdateInterval());
        var nearest = FindNearestEnemy(player, out var distance);
        if (nearest is null)
        {
            ClearReveal(player, state);
            SetStatus(context, state, "📏 扫描中…", "#FFFFFF");
            return;
        }

        var threshold = FinitePositiveOr(_settings.XrayDistanceThreshold, 500.0f);
        var isRevealed = distance <= threshold;
        if (isRevealed)
        {
            if (state.RevealedTarget != nearest.Index)
            {
                _wallhack.SetTargetedGrant(
                    GrantId(player),
                    new[] { (player.Index, nearest.Index) });
                state.RevealedTarget = nearest.Index;
            }
        }
        else
        {
            ClearReveal(player, state);
        }

        var meters = distance / FinitePositiveOr(_settings.UnitsPerMeter, 100.0f);
        var color = meters <= 5.0f ? "#FF0000" : meters <= 10.0f ? "#FFAA00" : "#00FF00";
        var suffix = isRevealed ? " ⚠️ 透视标记！" : string.Empty;
        SetStatus(context, state, $"📏 最近敌人：{meters:F1}m{suffix}", color);
    }

    private void ClearReveal(CCSPlayerController player, RangeFinderState state)
    {
        if (state.RevealedTarget is null)
        {
            return;
        }

        _wallhack.RemoveGrant(GrantId(player));
        state.RevealedTarget = null;
    }

    private static void SetStatus(
        in SkillContext context,
        RangeFinderState state,
        string text,
        string color)
    {
        context.Plugin.RuntimePresentation.SetStatusLine(context.Player, StatusSource, text, color);
        state.StatusVisible = true;
    }

    private static CCSPlayerController? FindNearestEnemy(
        CCSPlayerController player,
        out float nearestDistance)
    {
        nearestDistance = float.MaxValue;
        var origin = player.PlayerPawn.Value?.AbsOrigin;
        if (origin is null)
        {
            return null;
        }

        CCSPlayerController? nearest = null;
        var nearestSquared = float.MaxValue;
        foreach (var candidate in Utilities.GetPlayers())
        {
            if (!candidate.IsValid
                || !candidate.PawnIsAlive
                || candidate.Team == player.Team
                || candidate.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
            {
                continue;
            }

            var candidateOrigin = candidate.PlayerPawn.Value?.AbsOrigin;
            if (candidateOrigin is null)
            {
                continue;
            }

            var x = candidateOrigin.X - origin.X;
            var y = candidateOrigin.Y - origin.Y;
            var z = candidateOrigin.Z - origin.Z;
            var distanceSquared = (x * x) + (y * y) + (z * z);
            if (distanceSquared >= nearestSquared)
            {
                continue;
            }

            nearestSquared = distanceSquared;
            nearest = candidate;
        }

        if (nearest is not null)
        {
            nearestDistance = MathF.Sqrt(nearestSquared);
        }

        return nearest;
    }

    private double UpdateInterval() =>
        FinitePositiveOr(_settings.UpdateIntervalSeconds, 0.15f);

    private static float FinitePositiveOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static string GrantId(CCSPlayerController player) =>
        $"skill:RangeFinder:{player.Index}";
}
