using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class DeafSkill : ISkill, IPlayerDeathSkill
{
    private sealed class DeafState
    {
        public string Owner { get; } = $"Deaf:{Guid.NewGuid():N}";
        public uint? TargetIndex { get; set; }
        public bool Used { get; set; }
        public bool Revoked { get; set; }
    }

    private readonly DeafSoundService _sounds;

    public DeafSkill(DeafSoundService sounds)
    {
        _sounds = sounds;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Deaf",
        DisplayName = "🔇 致聋",
        Description = "选择一名敌人，使其在本回合内听不到服务器发送的游戏声音。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "targeted-sound-debuff"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new DeafState();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => ReleaseTarget(state, notifyTarget: false));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<DeafState>(out var state) || state.Revoked)
        {
            return;
        }

        if (state.Used)
        {
            PluginText.Chat(context.Player, "[致聋] 本回合已经使用过该能力。");
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
            PluginText.Chat(caster, "[致聋] 当前没有可以选择的存活敌人。");
            return;
        }

        var menu = new WasdMenu(
            PluginText.Transform(caster, "🔇 致聋：选择要消除声音的敌人"),
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
        if (!context.State.TryGet<DeafState>(out var state))
        {
            return;
        }

        state.Revoked = true;
        context.Plugin.WasdMenus.Close(context.Player);
        ReleaseTarget(state, notifyTarget: true);
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (!context.State.TryGet<DeafState>(out var state)
            || state.TargetIndex != @event.Userid?.Index)
        {
            return;
        }

        ReleaseTarget(state, notifyTarget: false);
    }

    private void TrySelectTarget(CCSPlayerController caster, uint targetIndex, DeafState state)
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
            PluginText.Chat(caster, "[致聋] 目标已经失效，请重新选择。");
            return;
        }

        if (!_sounds.Mute(target, state.Owner))
        {
            PluginText.Chat(caster, "[致聋] 无法消除该目标的声音。");
            return;
        }

        state.TargetIndex = target.Index;
        state.Used = true;
        PluginText.Chat(caster, $"[致聋] {target.PlayerName} 已听不到游戏声音。");
        PluginText.Chat(target, $"[致聋] {caster.PlayerName} 使你失去了听觉！");
    }

    private void ReleaseTarget(DeafState state, bool notifyTarget)
    {
        if (state.TargetIndex is not { } targetIndex)
        {
            return;
        }

        state.TargetIndex = null;
        if (!_sounds.Release(targetIndex, state.Owner) || !notifyTarget)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is { IsValid: true, PawnIsAlive: true })
        {
            PluginText.Chat(target, "[致聋] 你的听觉已恢复正常。");
        }
    }
}
