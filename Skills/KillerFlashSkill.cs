using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class KillerFlashSkill : ISkill, IPlayerBlindSkill
{
    private readonly KillerFlashSettings _settings;
    private readonly AntiFlashSkill? _antiFlash;

    public KillerFlashSkill(KillerFlashSettings settings, AntiFlashSkill? antiFlash = null)
    {
        _settings = settings;
        _antiFlash = antiFlash;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "KillerFlash",
        DisplayName = "⚡ 杀手闪电",
        Description = "任何被你的闪光弹完全致盲的人都会死亡，包括你自己。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Epic,
        DefaultWeight = 10,
        MaxPerServer = 1,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "flashbang-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        if (!context.Player.IsValid || !context.Player.PawnIsAlive)
        {
            return;
        }

        var hasFlashbang = context.Player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_flashbang" }) == true;
        if (!hasFlashbang)
        {
            context.Player.GiveNamedItem("weapon_flashbang");
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
        var victim = @event.Userid;
        var attacker = @event.Attacker;
        if (victim is null
            || attacker is null
            || !victim.IsValid
            || !attacker.IsValid
            || attacker.Slot != context.Player.Slot
            || !victim.PawnIsAlive
            || _antiFlash?.IsHolder(victim) == true
            || (victim.Slot != attacker.Slot
                && victim.Team == attacker.Team
                && !_settings.FriendlyFire))
        {
            return;
        }

        var pawn = victim.PlayerPawn.Value;
        var threshold = float.IsFinite(_settings.MinimumFlashDuration)
            ? Math.Max(0.0f, _settings.MinimumFlashDuration)
            : 1.0f;
        if (pawn is not { IsValid: true } || pawn.FlashDuration < threshold)
        {
            return;
        }

        var damage = float.IsFinite(_settings.LethalDamage)
            ? Math.Max(1.0f, _settings.LethalDamage)
            : 9999.0f;
        if (!SkillDamage.TryDeal(attacker, victim, damage))
        {
            pawn.Health = 0;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Server.NextFrame(() =>
            {
                if (pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                {
                    pawn.CommitSuicide(false, true);
                }
            });
        }

        PluginText.ChatAll(victim.Slot == attacker.Slot
            ? $"⚡ {victim.PlayerName} 被自己的杀手闪电闪死了！"
            : $"⚡ {victim.PlayerName} 被 {attacker.PlayerName} 的杀手闪电击中！");
    }
}
