using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class GhoulSkill : ISkill, IPlayerDeathSkill
{
    private readonly GhoulSettings _settings;

    public GhoulSkill(GhoulSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Ghoul",
        DisplayName = "🧟 食尸鬼",
        Description = "每当其他玩家死亡，继承其兼容技能；最多拥有 5 个技能，主动技能最多 1 个。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "runtime-skill-collection"
        }
    };

    public void OnGranted(in SkillContext context)
    {
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        var deceased = @event.Userid;
        if (deceased is not { IsValid: true }
            || deceased.Index == context.Player.Index
            || !context.Player.IsValid
            || !context.Player.PawnIsAlive)
        {
            return;
        }

        var maximumSkills = Math.Clamp(_settings.MaximumSkills, 1, 8);
        var inherited = context.Plugin.RuntimeSkills.InheritSkillsFromPlayer(
            context.Player,
            deceased,
            Descriptor.Id,
            maximumSkills,
            out var totalSkills);
        if (inherited.Count == 0)
        {
            return;
        }

        var names = string.Join("、", inherited.Select(skill => skill.DisplayName));
        PluginText.Chat(
            context.Player,
            $"[食尸鬼] 🧟 从 {deceased.PlayerName} 身上继承：{names}（{totalSkills}/{maximumSkills}）");
        PluginText.Center(context.Player, $"🧟 吞噬技能：{names}");
    }
}
