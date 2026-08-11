using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class GodModeSkill : ISkill, IPreDamageSkill, ITickSkill
{
    private sealed class GodModeState
    {
        public bool Active { get; set; }
        public DateTime ActiveUntil { get; set; } = DateTime.MinValue;
        public Color? OriginalRender { get; set; }
    }

    private readonly GodModeSettings _settings;

    public GodModeSkill(GodModeSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "GodMode",
        DisplayName = "🛡️ 上帝模式",
        Description = "点击 css_useskill，在短时间内免疫所有伤害。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = -1,
        CooldownSeconds = 30.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-render-color-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new GodModeState());
        var duration = PositiveFiniteOr(_settings.DurationSeconds, 2.0f);
        PluginText.Chat(context.Player,
            $"[上帝模式] 点击 css_useskill 获得 {duration:0.#} 秒无敌，冷却 30 秒。");
    }

    public void OnActivated(in SkillContext context)
    {
        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is not { IsValid: true } || !context.Player.PawnIsAlive
            || !context.State.TryGet<GodModeState>(out var state))
        {
            return;
        }

        var duration = PositiveFiniteOr(_settings.DurationSeconds, 2.0f);
        state.OriginalRender ??= pawn.Render;
        state.Active = true;
        state.ActiveUntil = DateTime.UtcNow.AddSeconds(duration);

        var alpha = state.OriginalRender.Value.A;
        pawn.Render = Color.FromArgb(alpha, 255, 255, 0);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

        PluginText.Center(context.Player, $"🛡️ 无敌：{duration:0.#} 秒");
        PluginText.Chat(context.Player, $"[上帝模式] 已开启，持续 {duration:0.#} 秒！");
    }

    public void OnRevoked(in SkillContext context)
    {
        EndGodMode(context, false);
    }

    public void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo)
    {
        if (damageInfo.Damage <= 0.0f
            || !context.State.TryGet<GodModeState>(out var state)
            || !state.Active)
        {
            return;
        }

        if (DateTime.UtcNow >= state.ActiveUntil)
        {
            EndGodMode(context, true);
            return;
        }

        damageInfo.Damage = 0.0f;
    }

    public void OnTick(in SkillContext context)
    {
        if (context.State.TryGet<GodModeState>(out var state)
            && state.Active
            && DateTime.UtcNow >= state.ActiveUntil)
        {
            EndGodMode(context, true);
        }
    }

    private static void EndGodMode(in SkillContext context, bool notify)
    {
        if (!context.State.TryGet<GodModeState>(out var state) || !state.Active)
        {
            return;
        }

        state.Active = false;
        state.ActiveUntil = DateTime.MinValue;

        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is { IsValid: true } && state.OriginalRender is { } originalRender)
        {
            pawn.Render = originalRender;
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        state.OriginalRender = null;
        if (notify && context.Player.IsValid)
        {
            PluginText.Chat(context.Player, "[上帝模式] 无敌时间结束。");
        }
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;
}
