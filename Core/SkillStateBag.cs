using System.Diagnostics.CodeAnalysis;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Owns mutable runtime state for one concrete player-skill assignment.
/// States are isolated even when one player has multiple skills.
/// </summary>
public sealed class SkillStateBag
{
    private readonly Dictionary<Type, object> _states = new();

    public void Set<T>(T state) where T : class
    {
        ArgumentNullException.ThrowIfNull(state);
        _states[typeof(T)] = state;
    }

    public T GetOrCreate<T>() where T : class, new()
    {
        if (_states.TryGetValue(typeof(T), out var existing))
        {
            return (T)existing;
        }

        var created = new T();
        _states[typeof(T)] = created;
        return created;
    }

    public bool TryGet<T>([NotNullWhen(true)] out T? state) where T : class
    {
        if (_states.TryGetValue(typeof(T), out var existing))
        {
            state = (T)existing;
            return true;
        }

        state = null;
        return false;
    }

    public void Clear() => _states.Clear();
}
