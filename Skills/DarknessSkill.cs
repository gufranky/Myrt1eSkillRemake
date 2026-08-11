using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class DarknessSkill : ISkill
{
    private sealed class DarknessState
    {
        public bool Used { get; set; }
        public bool Revoked { get; set; }
    }

    private readonly DarknessService _darkness;

    public DarknessSkill(DarknessService darkness)
    {
        _darkness = darkness;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Darkness",
        DisplayName = "🌑 黑暗",
        Description = "对选定的敌人施加黑暗效果。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Rare,
        DefaultWeight = 10,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "targeted-vision-debuff",
            "screen-fade-vision"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new DarknessState());
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<DarknessState>(out var state) || state.Revoked)
        {
            return;
        }

        if (state.Used)
        {
            PluginText.Chat(context.Player, "[黑暗] 本回合已经使用过该能力。");
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
            PluginText.Chat(caster, "[黑暗] 当前没有可以选择的存活敌人。");
            return;
        }

        var menu = new CenterHtmlMenu(
            PluginText.Transform(caster, "🌑 黑暗：选择一名敌人"),
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
        if (context.State.TryGet<DarknessState>(out var state))
        {
            state.Revoked = true;
        }

        MenuManager.CloseActiveMenu(context.Player);
        _darkness.RemoveCaster(context.Player);
    }

    private void TrySelectTarget(
        CCSPlayerController caster,
        uint targetIndex,
        DarknessState state)
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
            PluginText.Chat(caster, "[黑暗] 目标已经失效，请重新选择。");
            return;
        }

        if (!_darkness.TryApply(caster, target))
        {
            PluginText.Chat(caster, "[黑暗] 无法对该目标施加黑暗效果。");
            return;
        }

        state.Used = true;
        PluginText.Chat(caster, $"[黑暗] 黑暗已经笼罩 {target.PlayerName}。");
        PluginText.Chat(target, "[黑暗] 让灯光熄灭……");
    }
}
