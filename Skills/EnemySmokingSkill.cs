using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

/// <summary>
/// Active skill: attach a visible smoke plume to every currently alive enemy
/// for thirty seconds. The effect is entity based, so it follows moving targets
/// and is cleaned up when the assignment, round, or map ends.
/// </summary>
public sealed class EnemySmokingSkill : ISkill
{
    private const float DurationSeconds = 30.0f;
    private const string ParticleName = "particles/explosions_fx/explosion_smokegrenade_init.vpcf";
    private const float PuffIntervalSeconds = 3.0f;
    private const float PuffDurationSeconds = 2.0f;

    private sealed class State
    {
        public bool Used { get; set; }
        public bool Revoked { get; set; }
        public List<CParticleSystem> Particles { get; } = new();
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "EnemySmoking",
        DisplayName = "烟雾标记",
        Description = "主动使用后，所有存活敌人头顶冒烟 30 秒。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "targeted-vision-visual",
            "particle-mark-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new State();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => RemoveParticles(state));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<State>(out var state) || state.Revoked || state.Used)
        {
            if (state?.Used == true)
            {
                PluginText.Chat(context.Player, "[烟雾标记] 本回合已经使用过。 ");
            }

            return;
        }

        var caster = context.Player;
        var enemies = Utilities.GetPlayers()
            .Where(player => player.IsValid
                             && player.PawnIsAlive
                             && player.Team != caster.Team
                             && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            .ToArray();

        if (enemies.Length == 0)
        {
            PluginText.Chat(caster, "[烟雾标记] 当前没有存活的敌人。 ");
            return;
        }

        state.Used = true;
        var effects = context.Effects;
        SpawnPuffs(enemies, state, effects);
        var repeat = context.Effects.AddTimer(
            PuffIntervalSeconds,
            () => SpawnPuffs(enemies, state, effects),
            TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        context.Effects.AddTimer(DurationSeconds, () =>
        {
            repeat.Kill();
            RemoveParticles(state);
        });
        PluginText.Chat(caster, $"[烟雾标记] {state.Particles.Count} 名敌人已被烟雾标记，持续 30 秒。 ");
    }

    public void OnRevoked(in SkillContext context)
    {
        if (context.State.TryGet<State>(out var state))
        {
            state.Revoked = true;
            RemoveParticles(state);
        }
    }

    private static void RemoveParticles(State state)
    {
        foreach (var particle in state.Particles)
        {
            if (particle is { IsValid: true })
            {
                particle.AcceptInput("Stop");
                particle.Remove();
            }
        }

        state.Particles.Clear();
    }

    private static void SpawnPuffs(
        IReadOnlyCollection<CCSPlayerController> enemies,
        State state,
        EffectScope effects)
    {
        foreach (var enemy in enemies)
        {
            if (!enemy.IsValid || !enemy.PawnIsAlive)
            {
                continue;
            }

            var pawn = enemy.PlayerPawn.Value;
            var origin = pawn?.AbsOrigin;
            if (pawn is not { IsValid: true } || origin is null)
            {
                continue;
            }

            var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (particle is not { IsValid: true })
            {
                continue;
            }

            particle.EffectName = ParticleName;
            particle.StartActive = true;
            particle.Teleport(new Vector(origin.X, origin.Y, origin.Z + 64.0f));
            particle.DispatchSpawn();
            particle.AcceptInput("SetParent", pawn, particle, "!activator");
            particle.AcceptInput("Start");
            state.Particles.Add(particle);
            effects.TrackEntity(particle);
            effects.AddTimer(PuffDurationSeconds, () =>
            {
                if (particle.IsValid)
                {
                    particle.AcceptInput("Stop");
                    particle.Remove();
                }
            });
        }
    }
}
