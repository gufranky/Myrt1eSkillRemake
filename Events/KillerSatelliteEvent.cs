using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class KillerSatelliteEvent : RoundEventBase
{
    public static readonly string[] GrantedSkillIds = ["KillerFlash", "Meito"];

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "KillerSatellite",
        DisplayName = "🛰️ 杀手卫星",
        Description = "所有人获得杀手闪电和名刀！致命闪光与名刀御守！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "skill-availability",
            "skill-selection-replace"
        }
    };

    public override void Contribute(RoundPlanBuilder builder) =>
        builder.ReplaceAllSkills(GrantedSkillIds);

    public override void OnApplied(in RoundEventContext context) =>
        PrintToChatAll("[娱乐事件] 🛰️ 杀手卫星：全员获得杀手闪电与名刀！致盲即死，名刀御命！");
}
