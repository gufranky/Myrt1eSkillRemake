using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class FlashJumpSkill : ISkill, IPlayerBlindSkill, IFlashbangDetonateSkill
{
    public const int InitialFlashbangCount = 1;

    private sealed class FlashJumpState
    {
        public int ReplenishmentsUsed { get; set; }
        public bool Active { get; set; } = true;
    }

    private readonly FlashJumpSettings _settings;
    private readonly AntiFlashSkill? _antiFlash;

    public FlashJumpSkill(FlashJumpSettings settings, AntiFlashSkill? antiFlash = null)
    {
        _settings = settings;
        _antiFlash = antiFlash;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "FlashJump",
        DisplayName = "✈️ 闪光跳跃",
        Description = "你的闪光弹会让敌人飞起来！致盲时间越长飞得越高！获得 1 颗闪光弹，投掷后最多补充 2 次！",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "flashbang-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new FlashJumpState();
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => state.Active = false);
        for (var i = 0; i < InitialFlashbangCount; i++)
        {
            GiveFlashbang(context.Player);
        }
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnPlayerBlind(in SkillContext context, EventPlayerBlind @event)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        if (attacker is not { IsValid: true }
            || victim is not { IsValid: true, PawnIsAlive: true }
            || attacker.Slot != context.Player.Slot
            || victim.Slot == attacker.Slot
            || victim.Team == attacker.Team
            || _antiFlash?.IsHolder(victim) == true)
        {
            return;
        }

        var pawn = victim.PlayerPawn.Value;
        if (pawn is not { IsValid: true } || pawn.FlashDuration <= 0.0f)
        {
            return;
        }

        var baseVelocity = FiniteOr(_settings.BaseJumpVelocity, 200.0f);
        var velocityPerSecond = FiniteOr(_settings.VelocityPerBlindSecond, 200.0f);
        var maximumVelocity = Math.Max(0.0f, FiniteOr(_settings.MaximumJumpVelocity, 800.0f));
        pawn.AbsVelocity.Z = CalculateJumpVelocity(
            pawn.FlashDuration,
            baseVelocity,
            velocityPerSecond,
            maximumVelocity);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_vecAbsVelocity");
        PluginText.Center(victim, "✈️ 你被闪光弹送上天了！");
    }

    public void OnFlashbangDetonate(in SkillContext context, EventFlashbangDetonate @event)
    {
        if (!context.State.TryGet<FlashJumpState>(out var state)
            || !state.Active
            || state.ReplenishmentsUsed >= Math.Clamp(_settings.MaximumReplenishments, 0, 10))
        {
            return;
        }

        state.ReplenishmentsUsed++;
        var player = context.Player;
        var effects = context.Effects;
        effects.AddTimer(0.01f, () =>
        {
            if (state.Active)
            {
                GiveFlashbang(player);
                PluginText.Chat(player,
                    $"[闪光跳跃] 闪光弹已补充（{state.ReplenishmentsUsed}/{Math.Clamp(_settings.MaximumReplenishments, 0, 10)}）");
            }
        });
    }

    private static void GiveFlashbang(CCSPlayerController player)
    {
        if (player.IsValid && player.PawnIsAlive)
        {
            player.GiveNamedItem("weapon_flashbang");
        }
    }

    public static float CalculateJumpVelocity(
        float blindDuration,
        float baseVelocity,
        float velocityPerSecond,
        float maximumVelocity)
    {
        var duration = float.IsFinite(blindDuration) ? Math.Max(0.0f, blindDuration) : 0.0f;
        var basis = float.IsFinite(baseVelocity) ? Math.Max(0.0f, baseVelocity) : 200.0f;
        var perSecond = float.IsFinite(velocityPerSecond) ? Math.Max(0.0f, velocityPerSecond) : 200.0f;
        var maximum = float.IsFinite(maximumVelocity) ? Math.Max(0.0f, maximumVelocity) : 800.0f;
        return Math.Clamp(basis + duration * perSecond, 0.0f, maximum);
    }

    private static float FiniteOr(float value, float fallback) => float.IsFinite(value) ? value : fallback;
}
