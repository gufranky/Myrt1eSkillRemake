namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// A pre-built random sequence whose entries are unique for one cycle. Entries
/// that are temporarily incompatible stay in the sequence for a later draw;
/// only a successfully drawn entry is consumed.
/// </summary>
public sealed class NonRepeatingSequence<T>
    where T : class
{
    private readonly Queue<T> _entries = new();

    public int Count => _entries.Count;

    public void Reset(IEnumerable<T> entries)
    {
        _entries.Clear();
        foreach (var entry in entries)
        {
            _entries.Enqueue(entry);
        }
    }

    public bool TryTake(Func<T, bool> canTake, out T? selected)
    {
        ArgumentNullException.ThrowIfNull(canTake);

        var attempts = _entries.Count;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var candidate = _entries.Dequeue();
            if (canTake(candidate))
            {
                selected = candidate;
                return true;
            }

            // It may become valid next round (for example, after an event or
            // team composition change), so do not burn it merely by probing.
            _entries.Enqueue(candidate);
        }

        selected = null;
        return false;
    }

    public void RemoveWhere(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (_entries.Count == 0)
        {
            return;
        }

        var retained = _entries.Where(entry => !predicate(entry)).ToArray();
        _entries.Clear();
        foreach (var entry in retained)
        {
            _entries.Enqueue(entry);
        }
    }
}
