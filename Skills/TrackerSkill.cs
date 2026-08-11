using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class TrackerSkill : ISkill, IPlayerDeathSkill
{
    private sealed class TrackerState
    {
        public string Owner { get; } = $"Tracker:{Guid.NewGuid():N}";
        public uint? TargetIndex { get; set; }
        public bool Used { get; set; }
        public bool Revoked { get; set; }
    }

    private readonly TrackerTrailService _tracker;

    public TrackerSkill(TrackerTrailService tracker)
    {
        _tracker = tracker;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Tracker",
        DisplayName = "🐾 追踪器",
        Description = "选择一名敌人，只有你能看到他身后留下的持续痕迹。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = 1,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "targeted-tracking-visual",
            "particle-trail-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new TrackerState();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => _tracker.Release(state.Owner));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<TrackerState>(out var state) || state.Revoked)
        {
            return;
        }

        if (state.Used)
        {
            PluginText.Chat(context.Player, "[追踪器] 本回合已经选择过目标。");
            return;
        }

        var caster = context.Player;
        var enemies = Utilities.GetPlayers()
            .Where(player => player.IsValid
                             && player.PawnIsAlive
                             && player.Team != caster.Team
                             && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            .ToArray();
        if (enemies.Length == 0)
        {
            PluginText.Chat(caster, "[追踪器] 当前没有可以选择的存活敌人。");
            return;
        }

        var menu = new WasdMenu(
            PluginText.Transform(caster, "🐾 追踪器：选择留下痕迹的敌人"),
            context.Plugin);
        foreach (var enemy in enemies)
        {
            var enemyIndex = enemy.Index;
            menu.AddMenuOption(
                PluginText.Transform(caster, enemy.PlayerName),
                (player, option) => TrySelectTarget(player, enemyIndex, state));
        }

        context.Plugin.WasdMenus.Open(caster, menu);
    }

    public void OnRevoked(in SkillContext context)
    {
        if (!context.State.TryGet<TrackerState>(out var state))
        {
            return;
        }

        state.Revoked = true;
        context.Plugin.WasdMenus.Close(context.Player);
        ReleaseTarget(state);
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (context.State.TryGet<TrackerState>(out var state)
            && state.TargetIndex == @event.Userid?.Index)
        {
            ReleaseTarget(state);
        }
    }

    private void TrySelectTarget(CCSPlayerController caster, uint targetIndex, TrackerState state)
    {
        if (state.Revoked || state.Used || !caster.IsValid || !caster.PawnIsAlive)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is not { IsValid: true, PawnIsAlive: true }
            || target.Team == caster.Team
            || target.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
        {
            PluginText.Chat(caster, "[追踪器] 目标已经失效，请重新选择。");
            return;
        }

        if (!_tracker.Apply(caster, target, state.Owner))
        {
            PluginText.Chat(caster, "[追踪器] 无法为该目标创建痕迹。");
            return;
        }

        state.TargetIndex = target.Index;
        state.Used = true;
        PluginText.Chat(caster, $"[追踪器] {target.PlayerName} 现在会留下只有你可见的痕迹。");
    }

    private void ReleaseTarget(TrackerState state)
    {
        state.TargetIndex = null;
        _tracker.Release(state.Owner);
    }
}
