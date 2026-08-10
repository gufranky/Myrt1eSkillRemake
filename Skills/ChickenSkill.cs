using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ChickenSkill : ISkill, ITickSkill, IPlayerDeathSkill
{
    private readonly ChickenService _chickens;

    public ChickenSkill(ChickenService chickens)
    {
        _chickens = chickens;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Chicken",
        DisplayName = "🐔 鸡",
        Description = "获得一只鸡的模型，移动速度提升 10%，并损失 50 点生命值。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "movement-speed",
            "player-model-control",
            "player-scale-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        if (!_chickens.Apply(context.Player))
        {
            throw new InvalidOperationException("Chicken could not replace the assigned player model.");
        }

        var controllerIndex = context.Player.Index;
        context.Effects.RegisterCleanup(() => _chickens.Remove(controllerIndex));
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        _chickens.Update(context.Player);
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid is { IsValid: true } victim && victim.Slot == context.Player.Slot)
        {
            _chickens.Remove(context.Player.Index, context.Player, restoreHealth: false);
        }
    }
}
