using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class AntiFlashSkill : ISkill, IPlayerBlindSkill
{
    private readonly AntiFlashSettings _settings;
    private readonly HashSet<uint> _holders = new();

    public AntiFlashSkill(AntiFlashSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "AntiFlash",
        DisplayName = "✨ 防闪光",
        Description = "免疫闪光弹，并使你的闪光弹致盲效果持续 7 秒。",
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
        var controllerIndex = context.Player.Index;
        _holders.Add(controllerIndex);
        context.Effects.RegisterCleanup(() => _holders.Remove(controllerIndex));

        if (!context.Player.IsValid || !context.Player.PawnIsAlive)
        {
            return;
        }

        var grenadeCount = Math.Clamp(_settings.GrenadeCount, 0, 10);
        for (var count = 0; count < grenadeCount; count++)
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

    public bool IsHolder(CCSPlayerController player) =>
        player.IsValid && _holders.Contains(player.Index);

    public void OnPlayerBlind(in SkillContext context, EventPlayerBlind @event)
    {
        var victim = @event.Userid;
        if (victim is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var pawn = victim.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        // Immunity always wins when both the thrower and victim have Anti-Flash.
        if (_holders.Contains(victim.Index))
        {
            pawn.FlashDuration = 0.0f;
            pawn.FlashMaxAlpha = 0.0f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flFlashDuration");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flFlashMaxAlpha");
            return;
        }

        var attacker = @event.Attacker;
        if (attacker is not { IsValid: true }
            || attacker.Index != context.Player.Index
            || !_holders.Contains(attacker.Index))
        {
            return;
        }

        pawn.FlashDuration = float.IsFinite(_settings.FlashDuration)
            ? Math.Max(0.0f, _settings.FlashDuration)
            : 7.0f;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flFlashDuration");
    }
}
