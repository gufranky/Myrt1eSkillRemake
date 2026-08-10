namespace Myrt1eSkill_Remake.Core;

public static class RaritySelector
{
    public static T? Select<T>(
        IReadOnlyCollection<T> candidates,
        Func<T, SkillRarity> getRarity,
        Func<SkillRarity, int> getRarityWeight,
        Func<T, int> getItemWeight,
        Random? random = null)
    {
        if (candidates.Count == 0)
        {
            return default;
        }

        var groups = candidates
            .GroupBy(getRarity)
            .Select(group => (Rarity: group.Key, Items: (IReadOnlyCollection<T>)group.ToArray()))
            .ToArray();

        var selectedGroup = WeightedSelector.Select(groups, group => getRarityWeight(group.Rarity), random);
        if (selectedGroup.Items is null)
        {
            return WeightedSelector.Select(candidates, getItemWeight, random);
        }

        return WeightedSelector.Select(selectedGroup.Items, getItemWeight, random);
    }
}

