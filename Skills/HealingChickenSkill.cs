using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class HealingChickenSkill : ISkill, ITickSkill, IPlayerDeathSkill
{
    private readonly HealingChickenService _chickens;

    public HealingChickenSkill(HealingChickenService chickens)
    {
        _chickens = chickens;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "HealingChicken",
        DisplayName = "💚 治愈鸡",
        Description = "三只小鸡会自动跟随你；它们在附近时分别持续为你治疗，也可以被敌人击杀。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Legendary,
        DefaultWeight = 10,
        MaxPerServer = 1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "healing-companion-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        if (!_chickens.Spawn(context.Player))
        {
            throw new InvalidOperationException("HealingChicken could not spawn a chicken companion.");
        }

        var ownerIndex = context.Player.Index;
        context.Effects.RegisterCleanup(() => _chickens.Remove(ownerIndex));
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context) => _chickens.Update(context.Player);

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid is { IsValid: true } victim && victim.Slot == context.Player.Slot)
        {
            _chickens.Remove(context.Player.Index);
        }
    }
}
