using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class BountyHunterSkill : ISkill, IPlayerDeathSkill
{
    private sealed class BountyHunterState
    {
        public uint? TargetIndex { get; set; }
    }

    private readonly BountyHunterSettings _settings;
    private readonly WallhackService _wallhack;

    public BountyHunterSkill(BountyHunterSettings settings, WallhackService wallhack)
    {
        _settings = settings;
        _wallhack = wallhack;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "BountyHunter",
        DisplayName = "🎯 赏金猎人",
        Description = "随机标记一名敌人并获得其位置透视；亲手击杀目标可获得生命与金钱奖励。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Rare,
        DefaultWeight = 10,
        MaxPerServer = 2,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bounty-targeting"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new BountyHunterState();
        context.State.Set(state);

        var target = FindRandomEnemy(context.Player);
        if (target is null)
        {
            PluginText.Chat(context.Player, "[赏金猎人] 当前没有可标记的存活敌人。");
            return;
        }

        state.TargetIndex = target.Index;
        var player = context.Player;
        var grantId = GrantId(player);
        _wallhack.SetTargetedGrant(grantId, new[] { (player.Index, target.Index) });
        context.Effects.RegisterCleanup(() => _wallhack.RemoveGrant(grantId));
        PluginText.Chat(player, $"[赏金猎人] 悬赏目标：{target.PlayerName}。将其击杀可获得生命和金钱奖励！");
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (!context.State.TryGet<BountyHunterState>(out var state)
            || state.TargetIndex != @event.Userid?.Index)
        {
            return;
        }

        _wallhack.RemoveGrant(GrantId(context.Player));
        state.TargetIndex = null;

        var attacker = @event.Attacker;
        if (attacker is not { IsValid: true }
            || attacker.Slot != context.Player.Slot
            || attacker.Slot == @event.Userid?.Slot)
        {
            PluginText.Chat(context.Player, "[赏金猎人] 悬赏目标已死亡，奖励失效。 ");
            return;
        }

        GrantReward(context.Player);
    }

    private void GrantReward(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn is { IsValid: true, Health: > 0 })
        {
            var maximumHealth = Math.Max(1, _settings.MaximumHealthAfterReward);
            if (pawn.MaxHealth < maximumHealth)
            {
                pawn.MaxHealth = maximumHealth;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            }

            pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + Math.Max(0, _settings.HealthReward));
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }

        var money = player.InGameMoneyServices;
        var moneyReward = Math.Max(0, _settings.MoneyReward);
        if (money is not null && moneyReward > 0)
        {
            var maximumMoney = Math.Max(0, ConVar.Find("mp_maxmoney")?.GetPrimitiveValue<int>() ?? 16000);
            money.Account = Math.Min(maximumMoney, money.Account + moneyReward);
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }

        PluginText.Chat(player, $"[赏金猎人] 悬赏完成：获得 +{Math.Max(0, _settings.HealthReward)} 生命和 ${moneyReward}！");
    }

    private static CCSPlayerController? FindRandomEnemy(CCSPlayerController owner)
    {
        var enemies = Utilities.GetPlayers()
            .Where(player => player.IsValid
                             && player.PawnIsAlive
                             && player.Team != owner.Team
                             && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            .ToArray();
        return enemies.Length == 0 ? null : enemies[Random.Shared.Next(enemies.Length)];
    }

    private static string GrantId(CCSPlayerController player) => $"skill:BountyHunter:{player.Index}";
}
