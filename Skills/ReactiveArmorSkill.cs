using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ReactiveArmorSkill : ISkill, IPreDamageSkill, ITickSkill
{
    private const string StatusSource = "skill:ReactiveArmor";

    private sealed class ReactiveArmorState
    {
        public int Charges { get; set; }
        public DateTime NextChargeAt { get; set; }
        public string LastStatusText { get; set; } = string.Empty;
    }

    private readonly ReactiveArmorSettings _settings;

    public ReactiveArmorSkill(ReactiveArmorSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "ReactiveArmor",
        DisplayName = "🛡️ 反应装甲",
        Description = "装甲抵消下一次完整伤害；每 15 秒获得一层，可叠加。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "first-hit-damage-negation"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var now = DateTime.UtcNow;
        var state = new ReactiveArmorState
        {
            Charges = Math.Max(0, _settings.InitialCharges),
            NextChargeAt = now.AddSeconds(GetRechargeSeconds())
        };
        context.State.Set(state);
        UpdateStatus(context, state, now, force: true);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
        context.Plugin.RuntimePresentation.RemoveStatusLine(context.Player, StatusSource);
    }

    public void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo)
    {
        if (damageInfo.Damage <= 0.0f
            || !context.State.TryGet<ReactiveArmorState>(out var state)
            || state.Charges <= 0)
        {
            return;
        }

        state.Charges--;
        damageInfo.Damage = 0.0f;
        damageInfo.TotalledDamage = 0.0f;
        damageInfo.ShouldBleed = false;
        UpdateStatus(context, state, DateTime.UtcNow, force: true);
        PluginText.Center(context.Player, $"🛡️ 反应装甲吸收伤害｜剩余 {state.Charges} 层");
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<ReactiveArmorState>(out var state))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var recharge = GetRechargeSeconds();
        var maximumCharges = GetMaximumCharges();
        while (now >= state.NextChargeAt)
        {
            state.Charges = AddCharge(state.Charges, maximumCharges);
            state.NextChargeAt = state.NextChargeAt.AddSeconds(recharge);
        }

        UpdateStatus(context, state, now);
    }

    public static int AddCharge(int currentCharges, int maximumCharges) =>
        maximumCharges < 0
            ? Math.Max(0, currentCharges) + 1
            : Math.Min(Math.Max(0, currentCharges) + 1, maximumCharges);

    private void UpdateStatus(
        in SkillContext context,
        ReactiveArmorState state,
        DateTime now,
        bool force = false)
    {
        var remaining = Math.Max(0.0, (state.NextChargeAt - now).TotalSeconds);
        var text = $"🛡️ 反应装甲：{state.Charges} 层｜下一层 {remaining:0.0}s";
        if (!force && text == state.LastStatusText)
        {
            return;
        }

        state.LastStatusText = text;
        context.Plugin.RuntimePresentation.SetStatusLine(
            context.Player,
            StatusSource,
            text,
            state.Charges > 0 ? "#65E572" : "#FF6961");
    }

    private double GetRechargeSeconds() =>
        float.IsFinite(_settings.RechargeSeconds)
            ? Math.Clamp(_settings.RechargeSeconds, 0.1f, 3600.0f)
            : 15.0f;

    private int GetMaximumCharges() =>
        _settings.MaximumCharges < 0 ? -1 : Math.Max(1, _settings.MaximumCharges);
}
