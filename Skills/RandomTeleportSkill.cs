using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class RandomTeleportSkill : ISkill
{
    public const float ActivationCooldownSeconds = 5.0f;

    private readonly NavMeshService _navMesh;

    public RandomTeleportSkill(NavMeshService navMesh)
    {
        _navMesh = navMesh;
    }

    public static SkillDescriptor Definition { get; } = new()
    {
        Id = "RandomTeleport",
        DisplayName = "🌀 随机传送",
        Description = "按 E 随机传送到一个可达的安全位置，冷却 5 秒。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Rare,
        DefaultWeight = 10,
        CooldownSeconds = ActivationCooldownSeconds,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-teleport-control"
        }
    };

    public SkillDescriptor Descriptor => Definition;

    public void OnGranted(in SkillContext context)
    {
    }

    public void OnActivated(in SkillContext context)
    {
        if (_navMesh.TryTeleportRandom(context.Player, out var failure))
        {
            PluginText.Center(context.Player, "🌀 随机传送！冷却 5 秒");
            return;
        }

        PluginText.Chat(context.Player, $"[随机传送] 传送失败：{failure}");
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
