using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class RamboSkill : ISkill
{
    private readonly RamboSettings _settings;

    public RamboSkill(RamboSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Rambo",
        DisplayName = "兰博",
        Description = "回合开始时获得随机数量的额外生命值。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "max-health-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid)
        {
            return;
        }

        var minimum = Math.Clamp(
            Math.Min(_settings.MinimumExtraHealth, _settings.MaximumExtraHealthExclusive - 1),
            0,
            999);
        var maximumExclusive = Math.Clamp(
            Math.Max(_settings.MaximumExtraHealthExclusive, minimum + 1),
            minimum + 1,
            1001);
        var bonus = Random.Shared.Next(minimum, maximumExclusive);
        var originalMaxHealth = pawn.MaxHealth;
        var player = context.Player;

        pawn.MaxHealth = Math.Min(pawn.Health + bonus, 1000);
        pawn.Health = pawn.MaxHealth;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        context.Effects.RegisterCleanup(() =>
        {
            var currentPawn = player.PlayerPawn.Value;
            if (currentPawn is null || !currentPawn.IsValid)
            {
                return;
            }

            currentPawn.MaxHealth = originalMaxHealth;
            currentPawn.Health = Math.Min(currentPawn.Health, originalMaxHealth);
            Utilities.SetStateChanged(currentPawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(currentPawn, "CBaseEntity", "m_iHealth");
        });

        PluginText.Chat(context.Player, $"[随机技能] 兰博：额外获得 {bonus} 点生命值");
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
