using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class NightmareSkill : ISkill
{
    private sealed class NightmareState
    {
        public bool Used { get; set; }
        public bool Revoked { get; set; }
    }

    private readonly NightmareService _nightmare;

    public NightmareSkill(NightmareService nightmare)
    {
        _nightmare = nightmare;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Nightmare",
        DisplayName = "梦魇",
        Description = "选择一名敌人，让其经历恐怖的视觉幻象。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Rare,
        DefaultWeight = 10,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "targeted-vision-debuff",
            "post-processing-vision"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new NightmareState());
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<NightmareState>(out var state) || state.Revoked)
        {
            return;
        }

        if (state.Used)
        {
            PluginText.Chat(context.Player, "[梦魇] 本回合已经使用过该能力。");
            return;
        }

        var casterTeam = context.Player.Team;
        var enemies = Utilities.GetPlayers()
            .Where(player => player.IsValid
                             && player.PawnIsAlive
                             && player.Team != casterTeam
                             && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            .ToArray();
        if (enemies.Length == 0)
        {
            PluginText.Chat(context.Player, "[梦魇] 当前没有可以选择的存活敌人。");
            return;
        }

        var menu = new CenterHtmlMenu(
            PluginText.Transform(context.Player, "梦魇：选择一名敌人"),
            context.Plugin);
        foreach (var enemy in enemies)
        {
            var enemyIndex = enemy.Index;
            menu.AddMenuOption(PluginText.Transform(context.Player, enemy.PlayerName), (caster, option) =>
                TrySelectTarget(caster, enemyIndex, state));
        }

        MenuManager.OpenCenterHtmlMenu(context.Plugin, context.Player, menu);
    }

    public void OnRevoked(in SkillContext context)
    {
        if (context.State.TryGet<NightmareState>(out var state))
        {
            state.Revoked = true;
        }

        MenuManager.CloseActiveMenu(context.Player);
        _nightmare.RemoveCaster(context.Player);
    }

    private void TrySelectTarget(CCSPlayerController caster, uint targetIndex, NightmareState state)
    {
        MenuManager.CloseActiveMenu(caster);
        if (state.Revoked || state.Used || !caster.IsValid || !caster.PawnIsAlive)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is not { IsValid: true, PawnIsAlive: true } || target.Team == caster.Team)
        {
            PluginText.Chat(caster, "[梦魇] 目标已经失效，请重新选择。");
            return;
        }

        if (!_nightmare.TryApply(caster, target))
        {
            PluginText.Chat(caster, "[梦魇] 无法对该目标施加幻象。");
            return;
        }

        state.Used = true;
        PluginText.Chat(caster, $"[梦魇] 你让 {target.PlayerName} 陷入了恐怖幻象。");
    }
}
