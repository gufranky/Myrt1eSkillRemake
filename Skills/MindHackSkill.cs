using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class MindHackSkill : ISkill, IPlayerDeathSkill
{
    private sealed class MindHackState
    {
        public string OwnerPrefix { get; } = $"MindHack:{Guid.NewGuid():N}";
        public Dictionary<uint, string> TargetOwners { get; } = new();
        public bool Used { get; set; }
        public bool Revoked { get; set; }
    }

    private readonly MindHackSettings _settings;
    private readonly MindHackService _mindHack;

    public MindHackSkill(MindHackSettings settings, MindHackService mindHack)
    {
        _settings = settings;
        _mindHack = mindHack;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "MindHack",
        DisplayName = "🧠 心灵黑客",
        Description = "使用后令所有存活敌人的前后与左右移动方向反转 15 秒！",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Rare,
        DefaultWeight = 10,
        MaxPerServer = -1,
        CooldownSeconds = 0.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "targeted-control-debuff",
            "movement-input-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new MindHackState();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => ReleaseAllTargets(state, false));
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<MindHackState>(out var state) || state.Revoked)
        {
            return;
        }

        if (state.Used)
        {
            PluginText.Chat(context.Player, "[心灵黑客] 本回合已经使用过该能力。");
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
            PluginText.Chat(caster, "[心灵黑客] 当前没有可影响的存活敌人。");
            return;
        }

        foreach (var enemy in enemies)
        {
            var owner = $"{state.OwnerPrefix}:{enemy.Index}";
            if (!_mindHack.Apply(enemy, owner))
            {
                continue;
            }

            state.TargetOwners[enemy.Index] = owner;
        }

        if (state.TargetOwners.Count == 0)
        {
            PluginText.Chat(caster, "[心灵黑客] 无法接管敌人的移动输入。");
            return;
        }

        var duration = PositiveFiniteOr(_settings.DurationSeconds, 15.0f);
        state.Used = true;
        PluginText.Chat(caster, $"[心灵黑客] 已反转全部 {state.TargetOwners.Count} 名存活敌人的移动方向，持续 {duration:0.#} 秒。");
        foreach (var targetIndex in state.TargetOwners.Keys)
        {
            var target = Utilities.GetPlayerFromIndex((int)targetIndex);
            if (target is { IsValid: true, PawnIsAlive: true })
            {
                PluginText.Chat(target, $"[心灵黑客] 你的前后、左右移动方向已被反转，持续 {duration:0.#} 秒！");
            }
        }

        context.Effects.AddTimer(duration, () => ReleaseAllTargets(state, true));
    }

    public void OnRevoked(in SkillContext context)
    {
        if (!context.State.TryGet<MindHackState>(out var state))
        {
            return;
        }

        state.Revoked = true;
        ReleaseAllTargets(state, false);
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (context.State.TryGet<MindHackState>(out var state)
            && @event.Userid is { } victim)
        {
            ReleaseTarget(state, victim.Index, false);
        }
    }

    private void ReleaseAllTargets(MindHackState state, bool notifyTargets)
    {
        foreach (var targetIndex in state.TargetOwners.Keys.ToArray())
        {
            ReleaseTarget(state, targetIndex, notifyTargets);
        }
    }

    private void ReleaseTarget(MindHackState state, uint targetIndex, bool notifyTarget)
    {
        if (!state.TargetOwners.Remove(targetIndex, out var owner))
        {
            return;
        }

        _mindHack.Release(owner);

        if (!notifyTarget)
        {
            return;
        }

        var target = Utilities.GetPlayerFromIndex((int)targetIndex);
        if (target is { IsValid: true, PawnIsAlive: true })
        {
            PluginText.Chat(target, "[心灵黑客] 你的移动方向已经恢复正常。");
        }
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;
}
