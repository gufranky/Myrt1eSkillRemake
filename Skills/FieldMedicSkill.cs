using CounterStrikeSharp.API;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class FieldMedicSkill : ISkill
{
    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "FieldMedic",
        DisplayName = "战地急救",
        Description = "主动使用后恢复 25 点生命，冷却 20 秒。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Uncommon,
        DefaultWeight = 10,
        MaxPerServer = 4,
        CooldownSeconds = 20,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "active-healing"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        PluginText.Chat(context.Player, "[随机技能] 按 E 或输入 !useskill 使用战地急救。");
    }

    public void OnActivated(in SkillContext context)
    {
        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid || pawn.Health <= 0)
        {
            return;
        }

        pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + 25);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        PluginText.Center(context.Player, "战地急救：生命已恢复");
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
