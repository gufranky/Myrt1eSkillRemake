using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Myrt1eSkill_Remake.Core;

public sealed class PerformanceMonitor
{
    private sealed class Aggregate
    {
        public double TotalMilliseconds;
        public double MaxMilliseconds;
        public int Samples;
        public DateTime WindowStartedAt = DateTime.UtcNow;
    }

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly Dictionary<string, Aggregate> _aggregates = new(StringComparer.Ordinal);

    public PerformanceMonitor(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
    }

    public void Measure(string operation, Action action)
    {
        if (!_plugin.Config.PerformanceLoggingEnabled)
        {
            action();
            return;
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            action();
        }
        finally
        {
            Record(operation, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    private void Record(string operation, double milliseconds)
    {
        if (!_aggregates.TryGetValue(operation, out var aggregate))
        {
            aggregate = new Aggregate();
            _aggregates[operation] = aggregate;
        }

        aggregate.TotalMilliseconds += milliseconds;
        aggregate.MaxMilliseconds = Math.Max(aggregate.MaxMilliseconds, milliseconds);
        aggregate.Samples++;

        if ((DateTime.UtcNow - aggregate.WindowStartedAt).TotalSeconds < _plugin.Config.PerformanceReportSeconds)
        {
            return;
        }

        if (aggregate.MaxMilliseconds >= _plugin.Config.PerformanceWarningMilliseconds)
        {
            _plugin.Logger.LogWarning(
                "Performance {Operation}: avg={Average:F3}ms max={Maximum:F3}ms samples={Samples}",
                operation,
                aggregate.TotalMilliseconds / Math.Max(1, aggregate.Samples),
                aggregate.MaxMilliseconds,
                aggregate.Samples);
        }

        aggregate.TotalMilliseconds = 0;
        aggregate.MaxMilliseconds = 0;
        aggregate.Samples = 0;
        aggregate.WindowStartedAt = DateTime.UtcNow;
    }
}

