namespace Myrt1eSkill_Remake.Core;

public static class CompatibilityResolver
{
    public static bool CanCombine(IReadOnlyCollection<IRoundEvent> selected, IRoundEvent candidate)
    {
        var candidateDescriptor = candidate.Descriptor;

        foreach (var existing in selected)
        {
            var existingDescriptor = existing.Descriptor;
            if (existingDescriptor.Id.Equals(candidateDescriptor.Id, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (existingDescriptor.ExclusiveTags.Overlaps(candidateDescriptor.ExclusiveTags))
            {
                return false;
            }

            if (existingDescriptor.IncompatibleEventIds.Contains(candidateDescriptor.Id)
                || candidateDescriptor.IncompatibleEventIds.Contains(existingDescriptor.Id))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsSkillCompatible(SkillDescriptor skill, SkillPlan plan)
    {
        if (plan.BlockedSkillIds.Contains(skill.Id))
        {
            return false;
        }

        if (skill.ConflictTags.Overlaps(plan.BlockedSkillTags))
        {
            return false;
        }

        return !skill.IncompatibleEventIds.Overlaps(plan.ActiveEventIds);
    }
}

