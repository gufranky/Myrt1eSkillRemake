using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class GiantEvent : RoundEventBase, IRoundEventPlayerSpawn
{
    private sealed record State(CCSPlayerPawn Pawn, float Scale, int Health, int MaxHealth);
    private readonly GiantEventSettings _settings;
    private readonly Dictionary<int, State> _states = new();
    private bool _active;
    public GiantEvent(GiantEventSettings settings) => _settings = settings;
    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "Giant", DisplayName = "我是巨人", Description = "所有玩家体型变为 1.5 倍，并拥有 300 点生命值。", DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "player-scale-rules", "player-health-rules" },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "player-scale-control", "max-health-control" }
    };
    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        context.Effects.RegisterCleanup(() => { _active = false; RestoreAll(); });
        foreach (var player in Utilities.GetPlayers()) Apply(player);
        PrintToChatAll("[娱乐事件] 我是巨人：所有玩家体型 1.5 倍，生命值 300！");
    }
    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid; var effects = context.Effects;
        effects.AddTimer(0.1f, () => { if (_active) Apply(player); });
    }
    private void Apply(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true }) return;
        Restore(player.Slot);
        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true }) return;
        _states[player.Slot] = new State(pawn, pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.Scale ?? 1.0f, pawn.Health, pawn.MaxHealth);
        var scale = float.IsFinite(_settings.PlayerScale) ? Math.Clamp(_settings.PlayerScale, 1.0f, 3.0f) : 1.5f;
        var health = Math.Max(1, _settings.Health);
        var skeleton = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton is not null) skeleton.Scale = scale;
        pawn.AcceptInput("SetScale", pawn, pawn, scale.ToString(CultureInfo.InvariantCulture));
        pawn.MaxHealth = health; pawn.Health = health;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
    }
    private void RestoreAll() { foreach (var slot in _states.Keys.ToArray()) Restore(slot); }
    private void Restore(int slot)
    {
        if (!_states.Remove(slot, out var state) || !state.Pawn.IsValid) return;
        var skeleton = state.Pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton is not null) skeleton.Scale = state.Scale;
        state.Pawn.AcceptInput("SetScale", state.Pawn, state.Pawn, state.Scale.ToString(CultureInfo.InvariantCulture));
        state.Pawn.MaxHealth = state.MaxHealth;
        if (state.Pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE) state.Pawn.Health = Math.Min(state.Health, state.MaxHealth);
        Utilities.SetStateChanged(state.Pawn, "CBaseEntity", "m_CBodyComponent");
        Utilities.SetStateChanged(state.Pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(state.Pawn, "CBaseEntity", "m_iMaxHealth");
    }
}
