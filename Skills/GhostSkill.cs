using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class GhostSkill : ISkill, IPlayerHurtSkill, IPlayerDeathSkill
{
    private readonly GhostService _ghosts;

    public GhostSkill(GhostService ghosts)
    {
        _ghosts = ghosts;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Ghost",
        DisplayName = "👻 鬼",
        Description = "对敌人完全隐形；受到伤害或造成伤害后永久显形。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Epic,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-visibility-control",
            "player-model-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        if (!_ghosts.Hide(context.Player))
        {
            throw new InvalidOperationException("Ghost could not hide the assigned player.");
        }

        var controllerIndex = context.Player.Index;
        context.Effects.RegisterCleanup(() => _ghosts.Remove(controllerIndex));
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerHurt(in SkillContext context, EventPlayerHurt @event)
    {
        if (@event.DmgHealth <= 0 && @event.DmgArmor <= 0)
        {
            return;
        }

        var causedDamage = @event.Attacker is { IsValid: true } attacker
            && attacker.Slot == context.Player.Slot;
        var tookDamage = @event.Userid is { IsValid: true } victim
            && victim.Slot == context.Player.Slot;
        if ((causedDamage || tookDamage) && _ghosts.Reveal(context.Player))
        {
            PluginText.Center(context.Player, "👻 你已经显形！");
            PluginText.Chat(context.Player, causedDamage
                ? "[鬼] 你造成了伤害，隐形永久解除。"
                : "[鬼] 你受到了伤害，隐形永久解除。");
        }
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid is { IsValid: true } victim && victim.Slot == context.Player.Slot)
        {
            _ghosts.Reveal(context.Player);
        }
    }
}
