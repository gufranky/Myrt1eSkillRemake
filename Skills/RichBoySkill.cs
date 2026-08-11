using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class RichBoySkill : ISkill
{
    private readonly RichBoySettings _settings;

    public RichBoySkill(RichBoySettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "RichBoy",
        DisplayName = "💰 富家子弟",
        Description = "回合开始时获得 5000～15000 美元的随机奖金。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "money-bonus"
        },
        IncompatibleEventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Bankruptcy"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var money = context.Player.InGameMoneyServices;
        if (money is null)
        {
            return;
        }

        var minimum = Math.Clamp(Math.Min(_settings.MinimumMoney, _settings.MaximumMoney), 0, 1_000_000);
        var maximum = Math.Clamp(Math.Max(_settings.MinimumMoney, _settings.MaximumMoney), minimum, 1_000_000);
        var requestedBonus = Random.Shared.Next(minimum, maximum + 1);
        var maximumMoney = GetMaximumMoney();
        var grantedBonus = Math.Clamp(requestedBonus, 0, Math.Max(0, maximumMoney - money.Account));

        SetMoney(context.Player, money.Account + grantedBonus, 0, maximumMoney);
        PluginText.Chat(
            context.Player,
            $"[随机技能] 💰 富家子弟：获得 ${grantedBonus} 奖金，当前资金 ${money.Account}。"
        );
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    private static int GetMaximumMoney() =>
        Math.Max(0, ConVar.Find("mp_maxmoney")?.GetPrimitiveValue<int>() ?? 16000);

    private static void SetMoney(
        CCSPlayerController player,
        int amount,
        int minimum,
        int maximum)
    {
        var money = player.InGameMoneyServices;
        if (!player.IsValid || money is null)
        {
            return;
        }

        money.Account = Math.Clamp(amount, Math.Min(minimum, maximum), maximum);
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
    }
}
