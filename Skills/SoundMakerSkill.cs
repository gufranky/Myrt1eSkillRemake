using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class SoundMakerSkill : ISkill
{
    private readonly SoundMakerService _sounds;

    public SoundMakerSkill(SoundMakerService sounds)
    {
        _sounds = sounds;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "SoundMaker",
        DisplayName = "声音制造者",
        Description = "时不时地，你会听到敌方玩家的尖叫声。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10
    };

    public void OnGranted(in SkillContext context)
    {
        _sounds.Acquire(context.Player, context.Effects);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

}
