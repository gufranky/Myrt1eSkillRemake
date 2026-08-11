using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class JammerSkill : ISkill, IPlayerDeathSkill
{
    private sealed class JammerState
    {
        public string Owner { get; } = $"Jammer:{Guid.NewGuid():N}";
        public uint? TargetIndex { get; set; }
        public bool Used { get; set; }
        public bool Revoked { get; set; }
    }

    private readonly CrosshairSuppressionService _crosshairs;

    public JammerSkill(CrosshairSuppressionService crosshairs)
    {
        _crosshairs = crosshairs;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Jammer",
        DisplayName = "📡 干扰器",
        Description = "选择一名敌人，禁用他的准星！",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "targeted-crosshair-debuff"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new JammerState();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => ReleaseTarget(state, notifyTarget: false));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<JammerState>(out var state) || state.Revoked)
        {
            return;
        }

        if (state.Used)
        {
            PluginText.Chat(context.Player, "[干扰器] 本回合已经使用过该能力。");
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
            PluginText.Chat(caster, "[干扰器] 当前没有可以选择的存活敌人。");
            return;
        }

        var menu = new CenterHtmlMenu(
            PluginText.Transform(caster, "📡 干扰器：选择要禁用准星的敌人"),
            context.Plugin);
        foreach (var enemy in enemies)
        {
            var enemyIndex = enemy.Index;
            menu.AddMenuOption(
                PluginText.Transform(caster, enemy.PlayerName),
                (player, option) => TrySelectTarget(player, enemyIndex, state));
        }

        MenuManager.OpenCenterHtmlMenu(context.Plugin, caster, menu);
    }

    public void OnRevoked(in SkillContext context)
    {
        if (!context.State.TryGet<JammerState>(out var state))
        {
            return;
        }

        state.Revoked = true;
        MenuManager.CloseActiveMenu(context.Player);
        ReleaseTarget(state, notifyTarget: true);
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (!context.State.TryGet<JammerState>(out var state)
            || state.TargetIndex != @event.Userid?.Index)
        {
            return;
        }

        ReleaseTarget(state, notifyTarget: false);
    }

    private void TrySelectTarget(
        CCSPlayerController caster,
        uint targetIndex,
        JammerState state)
    {
        MenuManager.CloseActiveMenu(caster);
        if (state.Revoked || state.Used || !caster.IsValid || !caster.PawnIsAlive)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is not { IsValid: true, PawnIsAlive: true }
            || target.Team == caster.Team
            || target.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
        {
            PluginText.Chat(caster, "[干扰器] 目标已经失效，请重新选择。");
            return;
        }

        if (!_crosshairs.Hide(target, state.Owner))
        {
            PluginText.Chat(caster, "[干扰器] 无法禁用该目标的准星。");
            return;
        }

        state.TargetIndex = target.Index;
        state.Used = true;
        PluginText.Chat(caster, $"[干扰器] 已禁用 {target.PlayerName} 的准星。");
        PluginText.Chat(target, $"[干扰器] {caster.PlayerName} 禁用了你的准星！");
    }

    private void ReleaseTarget(JammerState state, bool notifyTarget)
    {
        if (state.TargetIndex is not { } targetIndex)
        {
            return;
        }

        state.TargetIndex = null;
        if (!_crosshairs.Release(targetIndex, state.Owner) || !notifyTarget)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is { IsValid: true, PawnIsAlive: true })
        {
            PluginText.Chat(target, "[干扰器] 你的准星已恢复正常。");
        }
    }
}
