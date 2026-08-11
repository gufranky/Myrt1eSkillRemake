using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class DeathNoteSkill : ISkill
{
    private sealed class DeathNoteState
    {
        public bool Used { get; set; }
        public bool Revoked { get; set; }
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "DeathNote",
        DisplayName = "💀 死神名册",
        Description = "选择一名玩家，你和他一起死亡！",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mutual-suicide"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new DeathNoteState());
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<DeathNoteState>(out var state) || state.Revoked)
        {
            return;
        }

        if (state.Used)
        {
            PluginText.Chat(context.Player, "[死神名册] 本回合已经使用过该能力。");
            return;
        }

        var caster = context.Player;
        var targets = Utilities.GetPlayers()
            .Where(player => player.IsValid
                             && player.PawnIsAlive
                             && player.Index != caster.Index
                             && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            .ToArray();
        if (targets.Length == 0)
        {
            PluginText.Chat(caster, "[死神名册] 当前没有可以选择的存活玩家。");
            return;
        }

        var menu = new CenterHtmlMenu(
            PluginText.Transform(caster, "💀 死神名册：选择同归于尽的玩家"),
            context.Plugin);
        foreach (var target in targets)
        {
            var targetIndex = target.Index;
            menu.AddMenuOption(
                PluginText.Transform(caster, target.PlayerName),
                (player, option) => TrySelectTarget(player, targetIndex, state));
        }

        MenuManager.OpenCenterHtmlMenu(context.Plugin, caster, menu);
    }

    public void OnRevoked(in SkillContext context)
    {
        if (context.State.TryGet<DeathNoteState>(out var state))
        {
            state.Revoked = true;
        }

        MenuManager.CloseActiveMenu(context.Player);
    }

    private static void TrySelectTarget(
        CCSPlayerController caster,
        uint targetIndex,
        DeathNoteState state)
    {
        MenuManager.CloseActiveMenu(caster);
        if (state.Revoked || state.Used || !caster.IsValid || !caster.PawnIsAlive)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is not { IsValid: true, PawnIsAlive: true } || target.Index == caster.Index)
        {
            PluginText.Chat(caster, "[死神名册] 目标已经失效，请重新选择。");
            return;
        }

        var casterPawn = caster.PlayerPawn.Value;
        var targetPawn = target.PlayerPawn.Value;
        if (casterPawn is not { IsValid: true } || targetPawn is not { IsValid: true })
        {
            PluginText.Chat(caster, "[死神名册] 无法带走该目标。");
            return;
        }

        state.Used = true;
        var casterName = caster.PlayerName;
        var targetName = target.PlayerName;
        PluginText.Chat(caster, $"[死神名册] 你选择了 {targetName}，你们将一起死亡！");
        PluginText.Chat(target, $"[死神名册] {casterName} 选择与你同归于尽！");
        PluginText.ChatAll($"[死神名册] 💀 {casterName} 与 {targetName} 同归于尽！");

        targetPawn.CommitSuicide(false, true);
        casterPawn.CommitSuicide(false, true);
    }
}
