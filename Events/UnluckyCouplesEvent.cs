using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class UnluckyCouplesEvent : RoundEventBase, IRoundEventPreDamage
{
    private const string GrantId = "event:UnluckyCouples";
    private readonly UnluckyCouplesSettings _settings;
    private readonly WallhackService _wallhack;
    private readonly Dictionary<uint, uint> _partners = new();

    public UnluckyCouplesEvent(UnluckyCouplesSettings settings, WallhackService wallhack)
    {
        _settings = settings;
        _wallhack = wallhack;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "UnluckyCouples",
        DisplayName = "💑 苦命鸳鸯",
        Description = "玩家跨阵营两两配对；配对双方互相透视，且彼此造成的伤害增加。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "xray-vision-rules",
            "player-model-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-outline-vision"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _partners.Clear();
        var terrorists = ShuffleTeam(CsTeam.Terrorist);
        var counterTerrorists = ShuffleTeam(CsTeam.CounterTerrorist);
        var pairCount = Math.Min(terrorists.Count, counterTerrorists.Count);

        for (var index = 0; index < pairCount; index++)
        {
            var terrorist = terrorists[index];
            var counterTerrorist = counterTerrorists[index];
            _partners[terrorist.Index] = counterTerrorist.Index;
            _partners[counterTerrorist.Index] = terrorist.Index;
            PluginText.Chat(terrorist, $"💑 你的配对对象是：{counterTerrorist.PlayerName}");
            PluginText.Chat(counterTerrorist, $"💑 你的配对对象是：{terrorist.PlayerName}");
        }

        foreach (var ignored in terrorists.Skip(pairCount).Concat(counterTerrorists.Skip(pairCount)))
        {
            PluginText.Chat(ignored, "💑 你没有配对对象，本回合将被忽略。");
        }

        _wallhack.SetTargetedGrant(
            GrantId,
            _partners.Select(pair => (Viewer: pair.Key, Target: pair.Value)));
        context.Effects.RegisterCleanup(() =>
        {
            _wallhack.RemoveGrant(GrantId);
            _partners.Clear();
        });

        PrintToChatAll("[娱乐事件] 💑 苦命鸳鸯：玩家已跨阵营配对，配偶互相透视且彼此伤害翻倍！");
    }

    public void OnBeforeDamage(
        in RoundEventContext context,
        CCSPlayerController victim,
        CCSPlayerController attacker,
        CTakeDamageInfo damageInfo)
    {
        if (!_partners.TryGetValue(attacker.Index, out var partnerIndex)
            || partnerIndex != victim.Index
            || damageInfo.Damage <= 0.0f)
        {
            return;
        }

        var multiplier = float.IsFinite(_settings.DamageMultiplier)
            ? Math.Max(0.0f, _settings.DamageMultiplier)
            : 2.0f;
        damageInfo.Damage *= multiplier;
    }

    private static List<CCSPlayerController> ShuffleTeam(CsTeam team)
    {
        var players = Utilities.GetPlayers()
            .Where(player => player.IsValid && player.PawnIsAlive && player.Team == team)
            .ToList();

        for (var index = players.Count - 1; index > 0; index--)
        {
            var selected = Random.Shared.Next(index + 1);
            (players[index], players[selected]) = (players[selected], players[index]);
        }

        return players;
    }
}
