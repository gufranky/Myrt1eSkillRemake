using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class SoundMakerSkill : ISkill, ITickSkill
{
    private sealed class SoundMakerState
    {
        public DateTime NextPlaybackAt { get; set; }
    }

    private const string ScreamSound = "Hostage.Pain";
    private readonly SoundMakerSettings _settings;

    public SoundMakerSkill(SoundMakerSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "SoundMaker",
        DisplayName = "声音制造者",
        Description = "时不时地，你会听到敌方玩家的尖叫声。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new SoundMakerState
        {
            NextPlaybackAt = DateTime.UtcNow.AddSeconds(GetCooldownSeconds())
        });
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<SoundMakerState>(out var state)
            || !context.Player.IsValid
            || !context.Player.PawnIsAlive)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (now < state.NextPlaybackAt)
        {
            return;
        }

        state.NextPlaybackAt = now.AddSeconds(GetCooldownSeconds());
        var volume = float.IsFinite(_settings.SoundVolume)
            ? Math.Clamp(_settings.SoundVolume, 0.0f, 1.0f)
            : 1.0f;
        var listener = new RecipientFilter(context.Player);

        foreach (var enemy in Utilities.GetPlayers())
        {
            if (!enemy.IsValid
                || !enemy.PawnIsAlive
                || enemy.Team == context.Player.Team
                || enemy.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
            {
                continue;
            }

            var pawn = enemy.PlayerPawn.Value;
            if (pawn is { IsValid: true })
            {
                pawn.EmitSound(ScreamSound, listener, volume);
            }
        }
    }

    private double GetCooldownSeconds() =>
        float.IsFinite(_settings.CooldownSeconds)
            ? Math.Max(0.1f, _settings.CooldownSeconds)
            : 2.0f;
}
