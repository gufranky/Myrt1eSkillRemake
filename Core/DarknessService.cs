using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.UserMessages;
using Myrt1eSkill_Remake.Configuration;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Owns persistent per-player Fade messages. Effects are tracked per caster so
/// removing one Darkness assignment cannot clear another caster's overlay.
/// </summary>
public sealed class DarknessService : IDisposable
{
    public const float RefreshIntervalSeconds = 5.0f;
    public const int FadeDuration = 100;
    public const int FadeHoldTime = 3000;
    public const int ClearDuration = 200;

    private sealed record DarknessEffect(
        uint CasterIndex,
        uint TargetIndex,
        Timer RefreshTimer);

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly DarknessSettings _settings;
    private readonly Dictionary<uint, DarknessEffect> _byCaster = new();
    private readonly Dictionary<uint, Dictionary<uint, DarknessEffect>> _byTarget = new();
    private bool _disposed;

    public DarknessService(Myrt1eSkillRemakePlugin plugin, DarknessSettings settings)
    {
        _plugin = plugin;
        _settings = settings;
    }

    public bool TryApply(CCSPlayerController caster, CCSPlayerController target)
    {
        if (_disposed
            || !caster.IsValid
            || !caster.PawnIsAlive
            || !target.IsValid
            || !target.PawnIsAlive
            || caster.Team == target.Team)
        {
            return false;
        }

        RemoveCaster(caster, notifyTarget: false);
        DarknessEffect? effect = null;
        var timer = _plugin.AddTimer(
            RefreshIntervalSeconds,
            () =>
            {
                if (effect is not null)
                {
                    Refresh(effect);
                }
            },
            TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        effect = new DarknessEffect(caster.Index, target.Index, timer);
        _byCaster[caster.Index] = effect;
        if (!_byTarget.TryGetValue(target.Index, out var targetEffects))
        {
            targetEffects = new Dictionary<uint, DarknessEffect>();
            _byTarget[target.Index] = targetEffects;
        }

        targetEffects[caster.Index] = effect;
        SendDarkness(target);
        return true;
    }

    public void RemoveCaster(CCSPlayerController? caster, bool notifyTarget = true)
    {
        if (caster is not null)
        {
            RemoveCaster(caster.Index, notifyTarget);
        }
    }

    public void RemoveCaster(uint casterIndex, bool notifyTarget = true)
    {
        if (_byCaster.TryGetValue(casterIndex, out var effect))
        {
            RemoveEffect(effect, notifyTarget);
        }
    }

    public void RemoveTarget(CCSPlayerController? target)
    {
        if (target is null || !_byTarget.TryGetValue(target.Index, out var effects))
        {
            return;
        }

        foreach (var effect in effects.Values.ToArray())
        {
            RemoveEffect(effect, notifyTarget: false, clearWhenLast: false);
        }

        if (target.IsValid)
        {
            SendClear(target);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var targetIndex in _byTarget.Keys.ToArray())
        {
            RemoveTarget(Utilities.GetPlayerFromIndex((int)targetIndex));
        }

        foreach (var effect in _byCaster.Values.ToArray())
        {
            effect.RefreshTimer.Kill();
        }

        _byCaster.Clear();
        _byTarget.Clear();
    }

    public static int PackColor(int red, int green, int blue, int alpha)
    {
        var r = Math.Clamp(red, 0, 255);
        var g = Math.Clamp(green, 0, 255);
        var b = Math.Clamp(blue, 0, 255);
        var a = Math.Clamp(alpha, 0, 255);
        return unchecked((a << 24) | (b << 16) | (g << 8) | r);
    }

    private void Refresh(DarknessEffect effect)
    {
        if (!_byCaster.TryGetValue(effect.CasterIndex, out var active) || active != effect)
        {
            effect.RefreshTimer.Kill();
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)effect.TargetIndex);
        if (target is { IsValid: true, PawnIsAlive: true })
        {
            SendDarkness(target);
        }
    }

    private void RemoveEffect(
        DarknessEffect effect,
        bool notifyTarget,
        bool clearWhenLast = true)
    {
        if (!_byCaster.TryGetValue(effect.CasterIndex, out var active) || active != effect)
        {
            return;
        }

        _byCaster.Remove(effect.CasterIndex);
        effect.RefreshTimer.Kill();
        if (!_byTarget.TryGetValue(effect.TargetIndex, out var targetEffects))
        {
            return;
        }

        targetEffects.Remove(effect.CasterIndex);
        if (targetEffects.Count > 0)
        {
            return;
        }

        _byTarget.Remove(effect.TargetIndex);
        if (!clearWhenLast)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)effect.TargetIndex);
        if (target is not { IsValid: true })
        {
            return;
        }

        SendClear(target);
        if (notifyTarget && target.PawnIsAlive)
        {
            PluginText.Chat(target, "[黑暗] 灯光恢复了。");
        }
    }

    private void SendDarkness(CCSPlayerController target)
    {
        SendFade(
            target,
            PackColor(_settings.Red, _settings.Green, _settings.Blue, _settings.Alpha),
            FadeDuration,
            FadeHoldTime);
    }

    private static void SendClear(CCSPlayerController target)
    {
        SendFade(target, PackColor(0, 0, 0, 0), ClearDuration, 0);
    }

    private static void SendFade(
        CCSPlayerController target,
        int color,
        int duration,
        int holdTime)
    {
        using var message = UserMessage.FromPartialName("Fade");
        if (message is null)
        {
            return;
        }

        message.SetInt("duration", duration);
        message.SetInt("hold_time", holdTime);
        message.SetInt("flags", 1);
        message.SetInt("color", color);
        message.Send(target);
    }
}
