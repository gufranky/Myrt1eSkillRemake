using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class BombMinerSkill : ISkill, IGrenadeThrownSkill
{
    private sealed class BombMinerState
    {
        public required int GrenadesRemaining { get; set; }
    }

    private readonly BombMinerSettings _settings;
    private readonly BombMinerService _mines;

    public BombMinerSkill(BombMinerSettings settings, BombMinerService mines)
    {
        _settings = settings;
        _mines = mines;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "BombMiner",
        DisplayName = "炸弹矿工",
        Description = "高爆手雷会变成感应地雷，附近出现敌人时以更高伤害和更大范围爆炸。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hegrenade-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new BombMinerState
        {
            GrenadesRemaining = Math.Clamp(_settings.GrenadeLimit, 1, 10)
        });
        _mines.Acquire(context.Player, context.Effects);
        GiveGrenade(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnGrenadeThrown(in SkillContext context, EventGrenadeThrown @event)
    {
        if (!string.Equals(@event.Weapon, "hegrenade", StringComparison.OrdinalIgnoreCase)
            || !context.State.TryGet<BombMinerState>(out var state)
            || state.GrenadesRemaining <= 0)
        {
            return;
        }

        state.GrenadesRemaining--;
        if (state.GrenadesRemaining > 0)
        {
            GiveGrenade(context.Player);
        }
    }

    private static void GiveGrenade(CCSPlayerController player)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return;
        }

        var alreadyHasGrenade = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_hegrenade" }) == true;
        if (!alreadyHasGrenade)
        {
            player.GiveNamedItem("weapon_hegrenade");
        }
    }
}
