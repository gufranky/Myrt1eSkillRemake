using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;

namespace Myrt1eSkill_Remake.Core;

public sealed class RoundCoordinator
{
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly SkillManager _skillManager;
    private readonly RoundEventManager _eventManager;
    private readonly RoundPresentationService _presentation;
    private long _roundGeneration;

    public RoundCoordinator(
        Myrt1eSkillRemakePlugin plugin,
        SkillManager skillManager,
        RoundEventManager eventManager,
        RoundPresentationService presentation)
    {
        _plugin = plugin;
        _skillManager = skillManager;
        _eventManager = eventManager;
        _presentation = presentation;
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        var generation = ++_roundGeneration;
        _skillManager.RevokeAll();
        _presentation.Clear();
        var plan = _eventManager.StartRound();

        ScheduleReveal(generation, plan);
        return HookResult.Continue;
    }

    private void ScheduleReveal(long generation, RoundPlan plan)
    {
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .FirstOrDefault()?.GameRules;

        if (gameRules is null || gameRules.WarmupPeriod)
        {
            _plugin.AddTimer(1.0f, () =>
            {
                if (generation == _roundGeneration)
                {
                    ScheduleReveal(generation, plan);
                }
            });
            return;
        }

        var freezeTime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 0;
        var delay = CalculateRevealDelay(
            freezeTime,
            _plugin.Config.SkillTimeBeforeStart,
            gameRules.TeamIntroPeriod);

        _plugin.AddTimer(delay, () =>
        {
            if (generation != _roundGeneration)
            {
                return;
            }

            _skillManager.AssignAllPlayers(plan.Skills);
            _presentation.Reveal(plan);
            _plugin.Logger.LogInformation(
                "Assigned skills for round generation {Generation} using {EventCount} resolved events",
                generation,
                plan.Events.Count);
        });
    }

    public static float CalculateRevealDelay(
        float freezeTimeSeconds,
        float skillTimeBeforeStartSeconds,
        bool teamIntroPeriod) =>
        (teamIntroPeriod ? 7.0f : 0.0f)
        + Math.Max(freezeTimeSeconds - Math.Max(0, skillTimeBeforeStartSeconds), 0)
        + 0.3f;

    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        CancelPendingAssignment();
        _presentation.Clear();
        _skillManager.RevokeAll();
        _eventManager.EndRound();
        return HookResult.Continue;
    }

    public void CancelPendingAssignment()
    {
        _roundGeneration++;
    }
}
