using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class SmallButDeadlyEvent : RoundEventBase, IRoundEventPlayerSpawn
{
    private sealed class PlayerState
    {
        public required CCSPlayerPawn Pawn { get; init; }
        public required float Scale { get; init; }
        public required float VelocityModifier { get; init; }
        public required float MaxSpeed { get; init; }
        public required int Health { get; init; }
        public required int MaxHealth { get; init; }
    }

    private readonly SmallButDeadlySettings _settings;
    private readonly Dictionary<int, PlayerState> _states = new();
    private bool _active;

    public SmallButDeadlyEvent(SmallButDeadlySettings settings)
    {
        _settings = settings;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "SmallButDeadly",
        DisplayName = "☠️ 小而致命",
        Description = "所有玩家变为 0.5 倍体型、2 倍移速，并且只有 10 点生命！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-scale-rules",
            "movement-speed-rules",
            "player-health-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "movement-speed",
            "player-scale-control",
            "max-health-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        context.Effects.RegisterCleanup(() =>
        {
            _active = false;
            RestoreAll();
        });

        foreach (var player in Utilities.GetPlayers())
        {
            Apply(player);
        }

        PrintToChatAll("[娱乐事件] ☠️ 小而致命：0.5 倍体型、2 倍移速、只有 10 点生命！");
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid;
        context.Effects.AddTimer(0.1f, () =>
        {
            if (_active)
            {
                Apply(player);
            }
        });
    }

    private void Apply(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        Restore(player.Slot);
        var pawn = player.PlayerPawn.Value;
        var movement = pawn?.MovementServices;
        if (pawn is not { IsValid: true } || movement is null)
        {
            return;
        }

        _states[player.Slot] = new PlayerState
        {
            Pawn = pawn,
            Scale = GetScale(pawn),
            VelocityModifier = pawn.VelocityModifier,
            MaxSpeed = movement.Maxspeed,
            Health = pawn.Health,
            MaxHealth = pawn.MaxHealth
        };

        var scale = PositiveFiniteOr(_settings.PlayerScale, 0.50f);
        var speed = PositiveFiniteOr(_settings.SpeedMultiplier, 2.0f);
        var health = Math.Max(1, _settings.Health);
        SetScale(pawn, scale);
        pawn.VelocityModifier = speed;
        movement.Maxspeed = 240.0f * speed;
        pawn.MaxHealth = health;
        pawn.Health = health;
        MarkChanged(pawn);
    }

    private void RestoreAll()
    {
        foreach (var slot in _states.Keys.ToArray())
        {
            Restore(slot);
        }
    }

    private void Restore(int slot)
    {
        if (!_states.Remove(slot, out var state) || !state.Pawn.IsValid)
        {
            return;
        }

        var pawn = state.Pawn;
        SetScale(pawn, state.Scale);
        pawn.VelocityModifier = state.VelocityModifier;
        if (pawn.MovementServices is not null)
        {
            pawn.MovementServices.Maxspeed = state.MaxSpeed;
        }

        pawn.MaxHealth = state.MaxHealth;
        if (pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
        {
            pawn.Health = Math.Min(state.Health, state.MaxHealth);
        }
        MarkChanged(pawn);
    }

    private static float GetScale(CCSPlayerPawn pawn) =>
        pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.Scale ?? 1.0f;

    private static void SetScale(CCSPlayerPawn pawn, float scale)
    {
        var skeleton = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton is null)
        {
            return;
        }

        skeleton.Scale = scale;
        pawn.AcceptInput("SetScale", pawn, pawn, scale.ToString(CultureInfo.InvariantCulture));
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
    }

    private static void MarkChanged(CCSPlayerPawn pawn)
    {
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;
}
