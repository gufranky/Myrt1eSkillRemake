using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class AdaptiveDisguiseSkill : ISkill, IPlayerHurtSkill, IPlayerDeathSkill
{
    private sealed class DisguiseState
    {
        public CCSPlayerPawn? Pawn { get; set; }
        public string OriginalModel { get; set; } = string.Empty;
        public bool Disguised { get; set; }
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "AdaptiveDisguise",
        DisplayName = "🎭 自适应伪装",
        Description = "主动伪装成随机敌方玩家；受到伤害后恢复原样。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 30.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-model-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new DisguiseState();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => Restore(state));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<DisguiseState>(out var state))
        {
            return;
        }

        if (state.Disguised)
        {
            Restore(state);
            PluginText.Chat(context.Player, "[自适应伪装] 伪装已主动解除。");
            return;
        }

        var player = context.Player;
        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        var enemies = Utilities.GetPlayers()
            .Where(enemy => enemy is { IsValid: true, PawnIsAlive: true }
                            && enemy.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist
                            && enemy.Team != player.Team)
            .ToArray();
        if (enemies.Length == 0)
        {
            PluginText.Chat(player, "[自适应伪装] 当前没有可供伪装的存活敌人。");
            return;
        }

        var target = enemies[Random.Shared.Next(enemies.Length)];
        var targetPawn = target.PlayerPawn.Value;
        var originalModel = GetModelName(pawn);
        var disguiseModel = targetPawn is { IsValid: true } ? GetModelName(targetPawn) : string.Empty;
        if (string.IsNullOrWhiteSpace(originalModel) || string.IsNullOrWhiteSpace(disguiseModel))
        {
            PluginText.Chat(player, "[自适应伪装] 无法读取玩家模型，伪装失败。");
            return;
        }

        state.Pawn = pawn;
        state.OriginalModel = originalModel;
        state.Disguised = true;
        pawn.SetModel(disguiseModel);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_nModelIndex");
        pawn.EmitSound("GlassBottle.BulletImpact");
        PluginText.Center(player, "🎭 伪装成功！");
        PluginText.Chat(player, $"[自适应伪装] 你已伪装成 {target.PlayerName}。");
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerHurt(in SkillContext context, EventPlayerHurt @event)
    {
        if ((@event.DmgHealth <= 0 && @event.DmgArmor <= 0)
            || @event.Userid is not { IsValid: true } victim
            || victim.Slot != context.Player.Slot
            || !context.State.TryGet<DisguiseState>(out var state)
            || !state.Disguised)
        {
            return;
        }

        Restore(state);
        PluginText.Center(context.Player, "🎭 受到伤害，伪装解除！");
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid is { IsValid: true } victim
            && victim.Slot == context.Player.Slot
            && context.State.TryGet<DisguiseState>(out var state))
        {
            Restore(state);
        }
    }

    private static void Restore(DisguiseState state)
    {
        if (!state.Disguised)
        {
            return;
        }

        var pawn = state.Pawn;
        if (pawn is { IsValid: true } && !string.IsNullOrWhiteSpace(state.OriginalModel))
        {
            pawn.SetModel(state.OriginalModel);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_nModelIndex");
            pawn.EmitSound("GlassBottle.BulletImpact");
        }

        state.Pawn = null;
        state.OriginalModel = string.Empty;
        state.Disguised = false;
    }

    private static string GetModelName(CCSPlayerPawn pawn) =>
        pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState?.ModelName ?? string.Empty;
}
