using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace Myrt1eSkill_Remake.Core;

public sealed class DamageEventRouter
{
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly SkillManager _skills;
    private readonly ExplosiveProjectileService _explosions;
    private readonly RoundEventManager _events;
    private bool _hooked;

    public DamageEventRouter(
        Myrt1eSkillRemakePlugin plugin,
        SkillManager skills,
        ExplosiveProjectileService explosions,
        RoundEventManager events)
    {
        _plugin = plugin;
        _skills = skills;
        _explosions = explosions;
        _events = events;
    }

    public void Load()
    {
        if (_hooked)
        {
            return;
        }

        _plugin.RegisterListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
        _hooked = true;
    }

    public void Unload()
    {
        if (!_hooked)
        {
            return;
        }

        try
        {
            _plugin.RemoveListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(exception, "Failed to remove the pre-damage hook");
        }
        finally
        {
            _hooked = false;
        }
    }

    private HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo damageInfo)
    {
        if (entity is null || !entity.IsValid || damageInfo is null)
        {
            return HookResult.Continue;
        }

        var pawn = entity.As<CCSPlayerPawn>();
        if (pawn is null || !pawn.IsValid || pawn.DesignerName != "player")
        {
            return HookResult.Continue;
        }

        var victim = pawn.Controller.Value?.As<CCSPlayerController>();
        if (victim is null || !victim.IsValid || !victim.PawnIsAlive)
        {
            return HookResult.Continue;
        }

        var explosion = _explosions.ApplyTeamDamageModifier(victim, damageInfo);

        var attackerPawn = damageInfo.Attacker?.Value?.As<CCSPlayerPawn>();
        var attacker = attackerPawn?.Controller?.Value?.As<CCSPlayerController>();
        if (attacker is { IsValid: true } && attacker.Slot != victim.Slot)
        {
            _skills.DispatchForPlayer<IPreDamageAttackerSkill>(
                attacker,
                "Damage.Pre.AttackerSkills",
                (handler, context) => handler.OnBeforeDamageDealt(context, victim, damageInfo));
            _events.Dispatch<IRoundEventPreDamage>(
                "Damage.Pre.ActiveEvents",
                (handler, context) => handler.OnBeforeDamage(context, victim, attacker, damageInfo));
        }

        _skills.DispatchForPlayer<IPreDamageSkill>(
            victim,
            "Damage.Pre.VictimSkills",
            (handler, context) => handler.OnBeforeDamage(context, damageInfo));

        _explosions.RegisterLethalDamageCredit(victim, pawn, damageInfo, explosion);
        return HookResult.Continue;
    }
}
