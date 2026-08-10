using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class TimeRecallSkill : ISkill, ITickSkill
{
    private sealed record HistorySnapshot(
        DateTime CapturedAt,
        Vector Position,
        QAngle ViewAngles,
        int Health,
        int Armor);

    private sealed class TimeRecallState
    {
        public List<HistorySnapshot> Snapshots { get; } = new();
        public DateTime NextCaptureAt { get; set; }
    }

    private readonly TimeRecallSettings _settings;
    private readonly PlayerViewService _playerView;

    public TimeRecallSkill(TimeRecallSettings settings, PlayerViewService playerView)
    {
        _settings = settings;
        _playerView = playerView;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "TimeRecall",
        DisplayName = "⏪ 时间回溯",
        Description = "使用后返回 5 秒前的位置、视角、血量和护甲状态。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 15.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-teleport-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new TimeRecallState();
        context.State.Set(state);
        Capture(context.Player, state, DateTime.UtcNow);
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<TimeRecallState>(out var state)
            || state.Snapshots.Count == 0
            || !context.Player.IsValid
            || !context.Player.PawnIsAlive)
        {
            PluginText.Chat(context.Player, "[时间回溯] 暂时没有可用的历史记录。");
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        var targetTime = DateTime.UtcNow.AddSeconds(-GetHistorySeconds());
        var snapshot = state.Snapshots.LastOrDefault(item => item.CapturedAt <= targetTime)
            ?? state.Snapshots[0];

        pawn.Teleport(
            new Vector(snapshot.Position.X, snapshot.Position.Y, snapshot.Position.Z),
            new QAngle(snapshot.ViewAngles.X, snapshot.ViewAngles.Y, snapshot.ViewAngles.Z),
            new Vector(0.0f, 0.0f, 0.0f));
        _playerView.TrySet(
            pawn,
            new QAngle(snapshot.ViewAngles.X, snapshot.ViewAngles.Y, snapshot.ViewAngles.Z));

        pawn.Health = Math.Clamp(snapshot.Health, 1, Math.Max(1, pawn.MaxHealth));
        pawn.ArmorValue = Math.Max(0, snapshot.Armor);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        PluginText.Center(context.Player, "⏪ 时间已回溯！");
        PluginText.Chat(
            context.Player,
            $"[时间回溯] 已恢复至约 {GetHistorySeconds():0.#} 秒前：{pawn.Health} 生命、{pawn.ArmorValue} 护甲。");
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<TimeRecallState>(out var state)
            || !context.Player.IsValid
            || !context.Player.PawnIsAlive)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (now < state.NextCaptureAt)
        {
            return;
        }

        Capture(context.Player, state, now);
    }

    private void Capture(CCSPlayerController player, TimeRecallState state, DateTime now)
    {
        state.NextCaptureAt = now.AddSeconds(GetCaptureIntervalSeconds());
        var pawn = player.PlayerPawn.Value;
        if (!player.IsValid
            || !player.PawnIsAlive
            || pawn is not { IsValid: true }
            || pawn.AbsOrigin is null)
        {
            return;
        }

        state.Snapshots.Add(new HistorySnapshot(
            now,
            new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z),
            new QAngle(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z),
            pawn.Health,
            pawn.ArmorValue));

        var cutoff = now.AddSeconds(-GetHistorySeconds());
        while (state.Snapshots.Count > 1 && state.Snapshots[1].CapturedAt <= cutoff)
        {
            state.Snapshots.RemoveAt(0);
        }
    }

    private double GetHistorySeconds() =>
        float.IsFinite(_settings.HistorySeconds)
            ? Math.Max(0.1f, _settings.HistorySeconds)
            : 5.0f;

    private double GetCaptureIntervalSeconds() =>
        float.IsFinite(_settings.CaptureIntervalSeconds)
            ? Math.Clamp(_settings.CaptureIntervalSeconds, 0.05f, 1.0f)
            : 0.25f;
}
