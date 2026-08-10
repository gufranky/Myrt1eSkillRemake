using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class SwapOnHitEvent : RoundEventBase, IRoundEventPlayerHurt, IRoundEventTick
{
    private const float SwapCooldownSeconds = 0.5f;
    private readonly Dictionary<int, float> _cooldowns = new();

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "SwapOnHit",
        DisplayName = "击中交换",
        Description = "击中敌人时会交换位置！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hit-teleport-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hit-teleport-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _cooldowns.Clear();
        PrintToChatAll("[娱乐事件] 击中交换：击中敌人时会交换双方的位置和朝向！");
    }

    public override void OnRemoved(in RoundEventContext context)
    {
        _cooldowns.Clear();
    }

    public void OnPlayerHurt(in RoundEventContext context, EventPlayerHurt @event)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        if (!IsAlive(attacker) || !IsAlive(victim) || attacker!.Slot == victim!.Slot)
        {
            return;
        }

        if (attacker.TeamNum == victim.TeamNum)
        {
            return;
        }

        if (IsCoolingDown(attacker.Slot) || IsCoolingDown(victim.Slot))
        {
            return;
        }

        var attackerPawn = attacker.PlayerPawn.Value;
        var victimPawn = victim.PlayerPawn.Value;
        if (attackerPawn is not { IsValid: true } || victimPawn is not { IsValid: true })
        {
            return;
        }

        var attackerOrigin = attackerPawn.AbsOrigin;
        var victimOrigin = victimPawn.AbsOrigin;
        var attackerRotation = attackerPawn.AbsRotation;
        var victimRotation = victimPawn.AbsRotation;
        if (attackerOrigin is null || victimOrigin is null || attackerRotation is null || victimRotation is null)
        {
            return;
        }

        var attackerPosition = Copy(attackerOrigin);
        var victimPosition = Copy(victimOrigin);
        var attackerAngles = Copy(attackerRotation);
        var victimAngles = Copy(victimRotation);
        var stopped = new Vector(0.0f, 0.0f, 0.0f);

        attackerPawn.Teleport(victimPosition, victimAngles, stopped);
        victimPawn.Teleport(attackerPosition, attackerAngles, new Vector(0.0f, 0.0f, 0.0f));

        var expiresAt = Server.CurrentTime + SwapCooldownSeconds;
        _cooldowns[attacker.Slot] = expiresAt;
        _cooldowns[victim.Slot] = expiresAt;

        PluginText.Center(attacker, $"💫 位置交换！冷却 {SwapCooldownSeconds:0.0} 秒");
        PluginText.Center(victim, $"💫 位置交换！冷却 {SwapCooldownSeconds:0.0} 秒");
    }

    public void OnTick(in RoundEventContext context)
    {
        var currentTime = Server.CurrentTime;
        foreach (var slot in _cooldowns
                     .Where(pair => currentTime >= pair.Value)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _cooldowns.Remove(slot);
        }
    }

    private bool IsCoolingDown(int slot)
    {
        return _cooldowns.TryGetValue(slot, out var expiresAt) && Server.CurrentTime < expiresAt;
    }

    private static bool IsAlive(CCSPlayerController? player)
    {
        return player is { IsValid: true, PawnIsAlive: true };
    }

    private static Vector Copy(Vector vector) => new(vector.X, vector.Y, vector.Z);

    private static QAngle Copy(QAngle angle) => new(angle.X, angle.Y, angle.Z);
}
