using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class SecondChanceSkill : ISkill, IPreDamageSkill
{
    private sealed class SecondChanceState
    {
        public bool Used { get; set; }
        public int ProtectedUntilTick { get; set; } = -1;
    }

    private readonly SecondChanceSettings _settings;
    private readonly ReviveService _revives;

    public SecondChanceSkill(SecondChanceSettings settings, ReviveService revives)
    {
        _settings = settings;
        _revives = revives;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "SecondChance",
        DisplayName = "🔄 第二次机会",
        Description = "每回合有一次机会在受到致命伤害时复活。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "second-chance"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new SecondChanceState());
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnBeforeDamage(in SkillContext context, CTakeDamageInfo damageInfo)
    {
        if (!context.State.TryGet<SecondChanceState>(out var state))
        {
            return;
        }

        if (Server.TickCount <= state.ProtectedUntilTick)
        {
            damageInfo.Damage = 0.0f;
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        if (state.Used
            || pawn is not { IsValid: true }
            || !DamageEvaluation.WouldBeLethal(pawn, damageInfo)
            || !_revives.TryRevive(context.Player, Math.Max(1, _settings.ReviveHealth)))
        {
            return;
        }

        state.Used = true;
        state.ProtectedUntilTick = Server.TickCount + 4;
        damageInfo.Damage = 0.0f;
        PluginText.Center(context.Player, "🔄 第二次机会！");
        PluginText.ChatAll($"🔄 {context.Player.PlayerName} 使用第二次机会复活了！");
    }
}
