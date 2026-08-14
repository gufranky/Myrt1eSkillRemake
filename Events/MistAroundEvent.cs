using System.Drawing;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class MistAroundEvent : RoundEventBase, IRoundEventTick
{
    private readonly FogService _fog;

    public MistAroundEvent(FogService fog)
    {
        _fog = fog;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "MistAround",
        DisplayName = "迷雾缭绕",
        Description = "玩家只能看见附近区域，更远的地图会被浓雾遮挡。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "vision-environment"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "vision-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _fog.Enable(end: 900.0f, maxDensity: 1.0f, exponent: 1.0f, Color.Black);
        context.Effects.RegisterCleanup(_fog.Disable);
        PrintToChatAll("[娱乐事件] 迷雾缭绕：远处区域被浓雾遮挡，只能看见附近地图。");
    }

    public void OnTick(in RoundEventContext context) => _fog.Tick();
}
