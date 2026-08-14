using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class KingModeEvent : RoundEventBase, IRoundEventPlayerDeath
{
    private sealed record KingState(CCSPlayerPawn Pawn, int Health, int MaxHealth);

    private const string GrantId = "event:KingMode";
    private readonly KingModeEventSettings _settings;
    private readonly WallhackService _wallhack;
    private readonly Dictionary<int, KingState> _originalStates = new();
    private readonly HashSet<CsTeam> _eliminatedTeams = [];
    private uint? _terroristKing;
    private uint? _counterTerroristKing;
    private bool _active;

    public KingModeEvent(KingModeEventSettings settings, WallhackService wallhack)
    {
        _settings = settings;
        _wallhack = wallhack;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "KingMode",
        DisplayName = "👑 国王模式",
        Description = "双方各有一名高生命国王；敌方可透视国王位置，国王死亡则其全队阵亡。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-health-rules", "xray-vision-rules", "team-elimination-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "max-health-control", "player-outline-vision"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        _eliminatedTeams.Clear();
        _originalStates.Clear();
        _terroristKing = SelectKing(CsTeam.Terrorist)?.Index;
        _counterTerroristKing = SelectKing(CsTeam.CounterTerrorist)?.Index;

        ApplyKingHealth(_terroristKing);
        ApplyKingHealth(_counterTerroristKing);
        UpdateVisionGrant();
        context.Effects.RegisterCleanup(() =>
        {
            _active = false;
            _wallhack.RemoveGrant(GrantId);
            RestoreKings();
            _terroristKing = null;
            _counterTerroristKing = null;
            _eliminatedTeams.Clear();
        });

        PrintToChatAll("[娱乐事件] 👑 国王模式：保护己方国王，敌方国王位置已被透视！");
    }

    public void OnPlayerDeath(in RoundEventContext context, EventPlayerDeath @event)
    {
        if (!_active || @event.Userid is not { IsValid: true } victim)
        {
            return;
        }

        var team = GetKingTeam(victim.Index);
        if (team is null || !_eliminatedTeams.Add(team.Value))
        {
            return;
        }

        _wallhack.RemoveGrant(GrantId);
        PrintToChatAll($"[娱乐事件] 👑 {(team == CsTeam.Terrorist ? "T" : "CT")} 方国王阵亡，全队覆灭！");
        EliminateTeam(team.Value, victim.Index);
    }

    private CCSPlayerController? SelectKing(CsTeam team)
    {
        var candidates = Utilities.GetPlayers()
            .Where(player => player.IsValid && player.PawnIsAlive && player.Team == team)
            .ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        var king = candidates[Random.Shared.Next(candidates.Length)];
        PluginText.Chat(king, $"[国王模式] 你是本回合的国王！拥有 {GetKingHealth()} 点生命；你死亡时全队会阵亡。");
        return king;
    }

    private void ApplyKingHealth(uint? kingIndex)
    {
        var player = kingIndex.HasValue ? Utilities.GetPlayerFromIndex((int)kingIndex.Value) : null;
        var pawn = player?.PlayerPawn.Value;
        if (player is not { IsValid: true, PawnIsAlive: true } || pawn is not { IsValid: true })
        {
            return;
        }

        _originalStates[player.Slot] = new KingState(pawn, pawn.Health, pawn.MaxHealth);
        var health = GetKingHealth();
        pawn.MaxHealth = health;
        pawn.Health = health;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
    }

    private void UpdateVisionGrant()
    {
        var terroristViewers = Utilities.GetPlayers().Where(player => player.IsValid && player.Team == CsTeam.Terrorist);
        var counterTerroristViewers = Utilities.GetPlayers().Where(player => player.IsValid && player.Team == CsTeam.CounterTerrorist);
        var grants = new List<(uint Viewer, uint Target)>();
        if (_counterTerroristKing.HasValue)
        {
            grants.AddRange(terroristViewers.Select(player => (player.Index, _counterTerroristKing.Value)));
        }

        if (_terroristKing.HasValue)
        {
            grants.AddRange(counterTerroristViewers.Select(player => (player.Index, _terroristKing.Value)));
        }

        _wallhack.SetTargetedGrant(GrantId, grants);
    }

    private CsTeam? GetKingTeam(uint playerIndex)
    {
        if (_terroristKing == playerIndex)
        {
            return CsTeam.Terrorist;
        }

        return _counterTerroristKing == playerIndex ? CsTeam.CounterTerrorist : null;
    }

    private void EliminateTeam(CsTeam team, uint deadKingIndex)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player is not { IsValid: true, PawnIsAlive: true }
                || player.Team != team
                || player.Index == deadKingIndex)
            {
                continue;
            }

            player.PlayerPawn.Value?.CommitSuicide(false, true);
        }
    }

    private void RestoreKings()
    {
        foreach (var (_, state) in _originalStates)
        {
            if (!state.Pawn.IsValid)
            {
                continue;
            }

            state.Pawn.MaxHealth = state.MaxHealth;
            if (state.Pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
            {
                state.Pawn.Health = Math.Min(state.Health, state.MaxHealth);
            }

            Utilities.SetStateChanged(state.Pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(state.Pawn, "CBaseEntity", "m_iMaxHealth");
        }

        _originalStates.Clear();
    }

    private int GetKingHealth() => Math.Clamp(_settings.KingHealth, 1, 10_000);
}
