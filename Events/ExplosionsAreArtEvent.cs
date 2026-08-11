using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class ExplosionsAreArtEvent : RoundEventBase
{
    public const string GrantedSkillId = "ExplosiveShot";

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "ExplosionsAreArt",
        DisplayName = "💥 爆炸就是艺术",
        Description = "所有玩家只获得爆炸射击，子弹落点有概率发生爆炸！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "skill-availability",
            "skill-selection-replace"
        }
    };

    public override void Contribute(RoundPlanBuilder builder) =>
        builder.ReplaceAllSkills(GrantedSkillId);

    public override void OnApplied(in RoundEventContext context) =>
        PrintToChatAll("[娱乐事件] 💥 爆炸就是艺术：所有玩家获得爆炸射击！");
}
