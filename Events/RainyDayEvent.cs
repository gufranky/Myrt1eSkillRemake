using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class RainyDayEvent : RoundEventBase,
    IRoundEventTick,
    IRoundEventPlayerSpawn,
    IRoundEventCheckTransmit
{
    private sealed record VisualState(CCSPlayerPawn Pawn, Color Render, float ShadowStrength);

    private readonly RainyDaySettings _settings;
    private readonly Dictionary<uint, VisualState> _visualStates = new();
    private bool _active;
    private bool _visible;
    private float _phaseEndsAt;

    public RainyDayEvent(RainyDaySettings settings)
    {
        _settings = settings;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "RainyDay",
        DisplayName = "🌧️ 下雨天",
        Description = "所有玩家隐身，每隔随机 3–10 秒同步显形 2 秒。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-visibility-rules",
            "player-model-rules",
            "xray-vision-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-visibility-control",
            "player-model-control",
            "player-outline-vision"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        _visible = false;
        HideAllPlayers();
        ScheduleHiddenPhase();
        context.Effects.RegisterCleanup(() =>
        {
            _active = false;
            _visible = true;
            RestoreAllPlayers();
        });

        PrintToChatAll("[娱乐事件] 🌧️ 下雨天：所有玩家隐身，并会随机同步显形 2 秒！");
    }

    public void OnTick(in RoundEventContext context)
    {
        if (!_active || Server.CurrentTime < _phaseEndsAt)
        {
            return;
        }

        if (_visible)
        {
            _visible = false;
            HideAllPlayers();
            ScheduleHiddenPhase();
            PluginText.ChatAll("🌧️ 所有人重新进入隐身状态。");
            return;
        }

        _visible = true;
        RestoreAllPlayers();
        _phaseEndsAt = Server.CurrentTime + PositiveFiniteOr(_settings.VisibleDurationSeconds, 2.0f);
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                PluginText.Center(player, "⚡ 闪电照亮了所有人！");
            }
        }
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        if (_visible)
        {
            return;
        }

        var player = @event.Userid;
        context.Effects.AddTimer(0.1f, () =>
        {
            if (_active && !_visible && player is { IsValid: true, PawnIsAlive: true })
            {
                HidePlayer(player);
            }
        });
    }

    public void OnCheckTransmit(in RoundEventContext context, CCheckTransmitInfoList infoList)
    {
        if (!_active || _visible)
        {
            return;
        }

        var hiddenPlayers = Utilities.GetPlayers()
            .Where(player => player is { IsValid: true, PawnIsAlive: true })
            .ToArray();
        foreach (var (info, viewer) in infoList)
        {
            if (viewer is not { IsValid: true })
            {
                continue;
            }

            var observedHandle = viewer.Pawn.Value?.ObserverServices?.ObserverTarget?.Value?.Handle ?? nint.Zero;
            foreach (var hidden in hiddenPlayers)
            {
                if (viewer.Index == hidden.Index)
                {
                    continue;
                }

                var pawn = hidden.PlayerPawn.Value;
                if (pawn is not { IsValid: true }
                    || (observedHandle != nint.Zero && observedHandle == pawn.Handle))
                {
                    continue;
                }

                info.TransmitEntities.Remove(pawn.Index);
                HideWeapons(info, pawn);
            }
        }
    }

    private void ScheduleHiddenPhase()
    {
        var configuredMinimum = FiniteOr(_settings.MinimumHiddenSeconds, 3.0f);
        var configuredMaximum = FiniteOr(_settings.MaximumHiddenSeconds, 10.0f);
        var minimum = Math.Max(0.0f, Math.Min(configuredMinimum, configuredMaximum));
        var maximum = Math.Max(minimum, Math.Max(configuredMinimum, configuredMaximum));
        var duration = minimum + Random.Shared.NextSingle() * (maximum - minimum);
        _phaseEndsAt = Server.CurrentTime + duration;
    }

    private void HideAllPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player is { IsValid: true, PawnIsAlive: true })
            {
                HidePlayer(player);
            }
        }
    }

    private void HidePlayer(CCSPlayerController player)
    {
        RestorePlayer(player.Index);
        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        _visualStates[player.Index] = new VisualState(pawn, pawn.Render, pawn.ShadowStrength);
        pawn.Render = Color.FromArgb(0, pawn.Render.R, pawn.Render.G, pawn.Render.B);
        pawn.ShadowStrength = 0.0f;
        MarkVisualChanged(pawn);
    }

    private void RestoreAllPlayers()
    {
        foreach (var controllerIndex in _visualStates.Keys.ToArray())
        {
            RestorePlayer(controllerIndex);
        }
    }

    private void RestorePlayer(uint controllerIndex)
    {
        if (!_visualStates.Remove(controllerIndex, out var state) || !state.Pawn.IsValid)
        {
            return;
        }

        state.Pawn.Render = state.Render;
        state.Pawn.ShadowStrength = state.ShadowStrength;
        MarkVisualChanged(state.Pawn);
    }

    private static void HideWeapons(CCheckTransmitInfo info, CCSPlayerPawn pawn)
    {
        var weapons = pawn.WeaponServices;
        if (weapons is null)
        {
            return;
        }

        foreach (var weaponHandle in weapons.MyWeapons)
        {
            var weapon = weaponHandle.Value;
            if (weapon is { IsValid: true })
            {
                info.TransmitEntities.Remove(weapon.Index);
            }
        }
    }

    private static void MarkVisualChanged(CCSPlayerPawn pawn)
    {
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_flShadowStrength");
    }

    private static float FiniteOr(float value, float fallback) => float.IsFinite(value) ? value : fallback;

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) ? Math.Max(0.0f, value) : fallback;
}
