using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class DuplicatorSkill : ISkill
{
    private sealed class DuplicatorState
    {
        public bool Revoked { get; set; }
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Duplicator",
        DisplayName = "📋 复制者",
        Description = "选择一个敌人，复制他的技能！",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "skill-assignment-replacement"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new DuplicatorState());
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<DuplicatorState>(out var state) || state.Revoked)
        {
            return;
        }

        var copier = context.Player;
        var plugin = context.Plugin;
        var targets = Utilities.GetPlayers()
            .Where(player => player.IsValid
                             && player.PawnIsAlive
                             && player.Team != copier.Team
                             && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            .Select(player => new
            {
                Player = player,
                Skills = plugin.RuntimeSkills.GetAssignedSkills(player)
            })
            .Where(entry => entry.Skills.Count > 0)
            .ToArray();
        if (targets.Length == 0)
        {
            PluginText.Chat(copier, "[复制者] 当前没有拥有技能的存活敌人可以复制。");
            return;
        }

        var menu = new CenterHtmlMenu(
            PluginText.Transform(copier, "📋 复制者：选择一名敌人"),
            plugin);
        foreach (var entry in targets)
        {
            var targetIndex = entry.Player.Index;
            var skillNames = string.Join(" / ", entry.Skills.Select(skill => skill.DisplayName));
            var label = $"{entry.Player.PlayerName}：{skillNames}";
            menu.AddMenuOption(
                PluginText.Transform(copier, label),
                (player, option) => TryCopyTarget(plugin, player, targetIndex, state));
        }

        MenuManager.OpenCenterHtmlMenu(plugin, copier, menu);
    }

    public void OnRevoked(in SkillContext context)
    {
        if (context.State.TryGet<DuplicatorState>(out var state))
        {
            state.Revoked = true;
        }

        MenuManager.CloseActiveMenu(context.Player);
    }

    private static void TryCopyTarget(
        Myrt1eSkillRemakePlugin plugin,
        CCSPlayerController copier,
        uint targetIndex,
        DuplicatorState state)
    {
        MenuManager.CloseActiveMenu(copier);
        if (state.Revoked || !copier.IsValid || !copier.PawnIsAlive)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is not { IsValid: true, PawnIsAlive: true } || target.Team == copier.Team)
        {
            PluginText.Chat(copier, "[复制者] 目标已经失效，请重新选择。");
            return;
        }

        if (!plugin.RuntimeSkills.TryReplaceSkillsFromPlayer(
                copier,
                target,
                out var copiedSkills,
                out var error))
        {
            PluginText.Chat(copier, $"[复制者] 复制失败：{error}");
            return;
        }

        var names = string.Join(" / ", copiedSkills.Select(skill => skill.DisplayName));
        PluginText.Chat(copier, $"[复制者] 你复制了 {target.PlayerName} 的技能：{names}");
        PluginText.Chat(target, $"[复制者] {copier.PlayerName} 复制了你的技能。");
    }
}
