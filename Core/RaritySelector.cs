namespace Myrt1eSkill_Remake.Core;

public static class RaritySelector
{
    public static IReadOnlyList<T> BuildUniqueSequence<T>(
        IReadOnlyCollection<T> candidates,
        int length,
        Func<T, SkillRarity> getRarity,
        Func<SkillRarity, int> getRarityWeight,
        Func<T, int> getItemWeight,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(getRarity);
        ArgumentNullException.ThrowIfNull(getRarityWeight);
        ArgumentNullException.ThrowIfNull(getItemWeight);
        var remaining = candidates
            .Where(candidate => Math.Max(0, getItemWeight(candidate)) > 0)
            .ToList();
        var sequence = new List<T>(Math.Min(Math.Max(0, length), remaining.Count));

        while (sequence.Count < length && remaining.Count > 0)
        {
            var selected = Select(remaining, getRarity, getRarityWeight, getItemWeight, random);
            if (selected is null)
            {
                break;
            }

            sequence.Add(selected);
            remaining.Remove(selected);
        }

        return sequence;
    }

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

