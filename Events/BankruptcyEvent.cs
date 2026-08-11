using CounterStrikeSharp.API;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

/// <summary>
/// Resets every connected player's account to $800 once when the event starts.
/// The money is intentionally not restored when the event ends.
/// </summary>
public sealed class BankruptcyEvent : RoundEventBase
{
    public const int BankruptcyMoney = 800;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "Bankruptcy",
        DisplayName = "💸 全员破产",
        Description = "所有人都破产了！金币只有800！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "economy-reset"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsHLTV)
            {
                continue;
            }

            var moneyServices = player.InGameMoneyServices;
            if (moneyServices is null)
            {
                continue;
            }

            moneyServices.Account = BankruptcyMoney;
            Utilities.SetStateChanged(
                player,
                "CCSPlayerController",
                "m_pInGameMoneyServices");
            PluginText.Chat(player, $"[娱乐事件] 💸 全员破产！你的金币现在是 {BankruptcyMoney}。");
        }
    }
}
