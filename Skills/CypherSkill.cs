using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class CypherSkill : ISkill, ITickSkill, IPlayerDeathSkill
{
    private readonly CypherCameraService _cameras;

    public CypherSkill(CypherCameraService cameras)
    {
        _cameras = cameras;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Cypher",
        DisplayName = "📹 Cypher",
        Description = "点击 [css_useSkill] 部署或切换监控摄像头；摄像头被摧毁后 30 秒可重新部署。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "camera-view-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var ownerIndex = context.Player.Index;
        context.Effects.RegisterCleanup(() => _cameras.Remove(ownerIndex));
    }

    public void OnActivated(in SkillContext context)
    {
        var result = _cameras.Toggle(context.Player, out var cooldownRemaining);
        switch (result)
        {
            case CypherToggleResult.NoSurface:
                PluginText.Chat(context.Player, "[Cypher] 请瞄准有足够空间的墙面部署摄像头。");
                break;
            case CypherToggleResult.Cooldown:
                PluginText.Chat(context.Player, $"[Cypher] 新摄像头将在 {Math.Ceiling(cooldownRemaining):0} 秒后就绪。");
                break;
            case CypherToggleResult.Deployed:
                PluginText.Chat(context.Player, "[Cypher] 摄像头已部署并接入监控画面。");
                break;
            case CypherToggleResult.Entered:
                PluginText.Chat(context.Player, "[Cypher] 已切换到监控画面。");
                break;
            case CypherToggleResult.Exited:
                PluginText.Chat(context.Player, "[Cypher] 已返回第一人称视角。");
                break;
            case CypherToggleResult.Failed:
                PluginText.Chat(context.Player, "[Cypher] 当前无法使用摄像头。");
                break;
        }
    }

    public void OnTick(in SkillContext context)
    {
        _cameras.Update(context.Player);
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid?.Index == context.Player.Index)
        {
            _cameras.Remove(context.Player.Index, context.Player);
        }
    }

    public void OnRevoked(in SkillContext context)
    {
    }
}
