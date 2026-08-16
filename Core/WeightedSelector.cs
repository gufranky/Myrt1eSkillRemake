namespace Myrt1eSkill_Remake.Core;

public static class WeightedSelector
{
    public static IReadOnlyList<T> BuildUniqueSequence<T>(
        IReadOnlyCollection<T> candidates,
        int length,
        Func<T, int> getWeight,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(getWeight);
        var remaining = candidates
            .Where(candidate => Math.Max(0, getWeight(candidate)) > 0)
            .ToList();
        var sequence = new List<T>(Math.Min(Math.Max(0, length), remaining.Count));

        while (sequence.Count < length && remaining.Count > 0)
        {
            var selected = Select(remaining, getWeight, random);
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
        Func<T, int> getWeight,
        Random? random = null)
    {
        if (candidates.Count == 0)
        {
            return default;
        }

        var weighted = candidates
            .Select(item => (Item: item, Weight: Math.Max(0, getWeight(item))))
            .Where(entry => entry.Weight > 0)
            .ToArray();

        var totalWeight = weighted.Sum(entry => (long)entry.Weight);
        if (totalWeight <= 0)
        {
            return default;
        }

        var roll = (random ?? Random.Shared).NextInt64(totalWeight);
        long cursor = 0;

        foreach (var entry in weighted)
        {
            cursor += entry.Weight;
            if (roll < cursor)
            {
                return entry.Item;
            }
        }

        return weighted[^1].Item;
    }
}
