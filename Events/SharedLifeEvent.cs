using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class SharedLifeEvent : RoundEventBase, IRoundEventPreDamage, IRoundEventTick, IRoundEventPlayerSpawn
{
    private readonly Dictionary<CsTeam, float> _pools = new();
    private bool _active;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "SharedLife",
        DisplayName = "共享生命",
        Description = "每队共享一个生命池，队员受到的伤害会从全队生命池中扣除。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "damage-rules", "health-rules" }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        _pools.Clear();
        foreach (var team in new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist })
        {
            _pools[team] = Utilities.GetPlayers()
                .Where(player => player is { IsValid: true, PawnIsAlive: true } && player.Team == team)
                .Sum(player => (float)(player.PlayerPawn.Value?.Health ?? 100));
        }

        context.Effects.RegisterCleanup(() =>
        {
            _active = false;
            _pools.Clear();
        });
        PluginText.ChatAll("[娱乐事件] 共享生命：每队成员共用一个生命池！");
    }

    public void OnBeforeDamage(in RoundEventContext context, CCSPlayerController victim, CCSPlayerController attacker, CTakeDamageInfo damageInfo)
    {
        if (!_active || damageInfo.Damage <= 0 || !_pools.ContainsKey(victim.Team))
        {
            return;
        }

        var damage = Math.Min(_pools[victim.Team], damageInfo.Damage);
        _pools[victim.Team] = Math.Max(0, _pools[victim.Team] - damage);
        damageInfo.Damage = 0;
        SyncTeam(victim.Team);
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        if (!_active || @event.Userid is not { IsValid: true } player || !_pools.ContainsKey(player.Team))
        {
            return;
        }

        context.Effects.AddTimer(0.2f, () => SyncTeam(player.Team));
    }

    public void OnTick(in RoundEventContext context)
    {
        if (!_active)
        {
            return;
        }

        SyncTeam(CsTeam.Terrorist);
        SyncTeam(CsTeam.CounterTerrorist);
    }

    private void SyncTeam(CsTeam team)
    {
        if (!_pools.TryGetValue(team, out var pool))
        {
            return;
        }

        var members = Utilities.GetPlayers()
            .Where(player => player is { IsValid: true, PawnIsAlive: true } && player.Team == team)
            .ToArray();
        if (members.Length == 0)
        {
            return;
        }

        var share = pool / members.Length;
        foreach (var player in members)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn is not { IsValid: true }) continue;
            pawn.Health = (int)MathF.Floor(share);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
    }
}
