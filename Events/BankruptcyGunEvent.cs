using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

/// <summary>
/// Incoming damage is paid for with the victim's account. Players who cannot
/// afford a hit are killed immediately instead of taking the normal damage.
/// </summary>
public sealed class BankruptcyGunEvent : RoundEventBase, IRoundEventPlayerHurt
{
    private const int MoneyPerDamage = 25;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "BankruptcyGun",
        DisplayName = "💸 破产枪",
        Description = "受到伤害时按伤害值的 25 倍扣除金币；如果余额不足，立即死亡。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "damage-economy-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "damage-economy-control"
        }
    };

    public override void OnApplied(in RoundEventContext context) =>
        PrintToChatAll("[娱乐事件] 💸 破产枪：受到伤害将扣除伤害值 25 倍的金币，余额不足则立即死亡！");

    public void OnPlayerHurt(in RoundEventContext context, EventPlayerHurt @event)
    {
        var victim = @event.Userid;
        if (victim is not { IsValid: true, PawnIsAlive: true }
            || victim.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
        {
            return;
        }

        var damage = Math.Max(0, @event.DmgHealth) + Math.Max(0, @event.DmgArmor);
        if (damage <= 0)
        {
            return;
        }

        var costLong = (long)damage * MoneyPerDamage;
        var cost = (int)Math.Min(int.MaxValue, costLong);
        var money = victim.InGameMoneyServices;
        if (money is null || money.Account < cost)
        {
            victim.PlayerPawn.Value?.CommitSuicide(false, true);
            PluginText.Center(victim, "💸 破产枪：你付不起这次伤害！");
            return;
        }

        money.Account -= cost;
        Utilities.SetStateChanged(victim, "CCSPlayerController", "m_pInGameMoneyServices");
        PluginText.Center(victim, $"💸 -${cost}（剩余 ${money.Account}）");
    }
}
