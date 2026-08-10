using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Owns every temporary effect created by one player-skill assignment.
/// Cleanup runs once, in reverse registration order.
/// </summary>
public sealed class EffectScope : IDisposable
{
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly Stack<Action> _cleanup = new();
    private bool _disposed;

    public EffectScope(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
    }

    public void RegisterCleanup(Action cleanup)
    {
        ArgumentNullException.ThrowIfNull(cleanup);

        if (_disposed)
        {
            cleanup();
            return;
        }

        _cleanup.Push(cleanup);
    }

    public Timer AddTimer(float seconds, Action callback, TimerFlags? flags = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var timer = _plugin.AddTimer(seconds, callback, flags);
        RegisterCleanup(timer.Kill);
        return timer;
    }

    public T TrackEntity<T>(T entity) where T : CEntityInstance
    {
        RegisterCleanup(() =>
        {
            if (entity.IsValid)
            {
                entity.Remove();
            }
        });
        return entity;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        while (_cleanup.TryPop(out var cleanup))
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                _plugin.Logger.LogError(exception, "Failed to clean up a skill effect");
            }
        }
    }
}
