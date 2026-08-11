using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class MagnifierSkill : ISkill, IPlayerDeathSkill
{
    private sealed class MagnifierState
    {
        public string Owner { get; } = $"Magnifier:{Guid.NewGuid():N}";
        public uint? TargetIndex { get; set; }
        public bool Used { get; set; }
        public bool Revoked { get; set; }
    }

    private readonly MagnifierSettings _settings;
    private readonly FieldOfViewService _fieldOfView;

    public MagnifierSkill(MagnifierSettings settings, FieldOfViewService fieldOfView)
    {
        _settings = settings;
        _fieldOfView = fieldOfView;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Magnifier",
        DisplayName = "🔍 放大镜",
        Description = "选择一名敌人，强制放大其屏幕并缩小视野。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "targeted-vision-debuff",
            "player-fov-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new MagnifierState();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => ReleaseTarget(state, false));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<MagnifierState>(out var state) || state.Revoked)
        {
            return;
        }

        if (state.Used)
        {
            PluginText.Chat(context.Player, "[放大镜] 本回合已经使用过该能力。");
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
            PluginText.Chat(caster, "[放大镜] 当前没有可以选择的存活敌人。");
            return;
        }

        var menu = new WasdMenu(
            PluginText.Transform(caster, "🔍 放大镜：选择要缩小视野的敌人"),
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
        if (!context.State.TryGet<MagnifierState>(out var state))
        {
            return;
        }

        state.Revoked = true;
        context.Plugin.WasdMenus.Close(context.Player);
        ReleaseTarget(state, true);
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (context.State.TryGet<MagnifierState>(out var state)
            && state.TargetIndex == @event.Userid?.Index)
        {
            ReleaseTarget(state, false);
        }
    }

    private void TrySelectTarget(CCSPlayerController caster, uint targetIndex, MagnifierState state)
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
            PluginText.Chat(caster, "[放大镜] 目标已经失效，请重新选择。");
            return;
        }

        var fov = Math.Clamp(_settings.CustomFov, 20u, 130u);
        if (!_fieldOfView.Apply(target, state.Owner, fov))
        {
            PluginText.Chat(caster, "[放大镜] 无法修改该目标的视野。");
            return;
        }

        state.TargetIndex = target.Index;
        state.Used = true;
        PluginText.Chat(caster, $"[放大镜] 已将 {target.PlayerName} 的视野压缩至 {fov}。");
        PluginText.Chat(target, $"[放大镜] {caster.PlayerName} 强制放大了你的视野！");
    }

    private void ReleaseTarget(MagnifierState state, bool notifyTarget)
    {
        if (state.TargetIndex is not { } targetIndex)
        {
            return;
        }

        state.TargetIndex = null;
        if (!_fieldOfView.Release(targetIndex, state.Owner) || !notifyTarget)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is { IsValid: true, PawnIsAlive: true })
        {
            PluginText.Chat(target, "[放大镜] 你的视野已经恢复正常。");
        }
    }
}
