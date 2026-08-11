using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class DeactivatorSkill : ISkill
{
    private sealed class DeactivatorState
    {
        public bool Revoked { get; set; }
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Deactivator",
        DisplayName = "🚫 技能终止者",
        Description = "选择一名玩家，禁用他当前拥有的技能。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Epic,
        DefaultWeight = 10,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "skill-assignment-replacement"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new DeactivatorState());
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<DeactivatorState>(out var state) || state.Revoked)
        {
            return;
        }

        var caster = context.Player;
        var plugin = context.Plugin;
        var targets = Utilities.GetPlayers()
            .Where(player => player.IsValid
                             && player.PawnIsAlive
                             && player.Team != caster.Team
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
            PluginText.Chat(caster, "[技能终止者] 当前没有拥有技能的存活敌人。");
            return;
        }

        var menu = new WasdMenu(
            PluginText.Transform(caster, "🚫 技能终止者：选择一名敌人"),
            plugin);
        foreach (var entry in targets)
        {
            var targetIndex = entry.Player.Index;
            var skillNames = string.Join(" / ", entry.Skills.Select(skill => skill.DisplayName));
            menu.AddMenuOption(
                PluginText.Transform(caster, $"{entry.Player.PlayerName}：{skillNames}"),
                (player, option) => TryDeactivate(plugin, player, targetIndex, state));
        }

        plugin.WasdMenus.Open(caster, menu);
    }

    public void OnRevoked(in SkillContext context)
    {
        if (context.State.TryGet<DeactivatorState>(out var state))
        {
            state.Revoked = true;
        }

        context.Plugin.WasdMenus.Close(context.Player);
    }

    private static void TryDeactivate(
        Myrt1eSkillRemakePlugin plugin,
        CCSPlayerController caster,
        uint targetIndex,
        DeactivatorState state)
    {
        if (state.Revoked || !caster.IsValid || !caster.PawnIsAlive)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is not { IsValid: true, PawnIsAlive: true } || target.Team == caster.Team)
        {
            PluginText.Chat(caster, "[技能终止者] 目标已经失效，请重新选择。");
            return;
        }

        var casterName = caster.PlayerName;
        var targetName = target.PlayerName;
        if (!plugin.RuntimeSkills.TryDeactivatePlayerSkills(
                caster,
                target,
                "Deactivator",
                out var disabledSkills,
                out var error))
        {
            PluginText.Chat(caster, $"[技能终止者] 禁用失败：{error}");
            return;
        }

        var names = string.Join(" / ", disabledSkills.Select(skill => skill.DisplayName));
        PluginText.Chat(caster, $"[技能终止者] 已禁用 {targetName} 的技能：{names}");
        PluginText.Chat(target, $"[技能终止者] {casterName} 禁用了你的技能：{names}");
    }
}
