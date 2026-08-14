using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Emits a real hostage-pain sound from each living player, then filters its
/// sound user-message so only SoundMaker holders hear enemy emitters. This
/// mirrors jRandomSkills and avoids unreliable per-recipient EmitSound calls.
/// </summary>
public sealed class SoundMakerService
{
    private const string ScreamSound = "Hostage.Pain";
    private const uint ScreamSoundHash = 1876781570;
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly SoundMakerSettings _settings;
    private readonly HashSet<uint> _holders = [];
    private DateTime _nextPlaybackAt;
    private bool _loaded;

    public SoundMakerService(Myrt1eSkillRemakePlugin plugin, SoundMakerSettings settings)
    {
        _plugin = plugin;
        _settings = settings;
    }

    public void Load()
    {
        if (_loaded)
        {
            return;
        }

        _plugin.HookUserMessage(DeafSoundService.SoundEventMessageId, OnSoundEvent);
        _plugin.RegisterListener<Listeners.OnTick>(OnTick);
        _loaded = true;
    }

    public void Unload()
    {
        if (!_loaded)
        {
            return;
        }

        _plugin.UnhookUserMessage(DeafSoundService.SoundEventMessageId, OnSoundEvent);
        _plugin.RemoveListener<Listeners.OnTick>(OnTick);
        _holders.Clear();
        _nextPlaybackAt = DateTime.MinValue;
        _loaded = false;
    }

    public void Acquire(CCSPlayerController player, EffectScope effects)
    {
        _holders.Add(player.Index);
        effects.RegisterCleanup(() => _holders.Remove(player.Index));
    }

    private void OnTick()
    {
        if (_holders.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (now < _nextPlaybackAt)
        {
            return;
        }

        _nextPlaybackAt = now.AddSeconds(GetCooldownSeconds());
        var volume = float.IsFinite(_settings.SoundVolume)
            ? Math.Clamp(_settings.SoundVolume, 0.0f, 1.0f)
            : 1.0f;
        foreach (var player in Utilities.GetPlayers())
        {
            if (player is not { IsValid: true, PawnIsAlive: true }
                || player.PlayerPawn.Value is not { IsValid: true } pawn)
            {
                continue;
            }

            pawn.EmitSound(ScreamSound, volume: volume);
        }
    }

    private HookResult OnSoundEvent(UserMessage message)
    {
        if (_holders.Count == 0 || message.ReadUInt("soundevent_hash") != ScreamSoundHash)
        {
            return HookResult.Continue;
        }

        var sourceEntityIndex = message.ReadUInt("source_entity_index");
        var emitter = Utilities.GetPlayers().FirstOrDefault(player =>
            player.IsValid
            && player.PlayerPawn.Value is { IsValid: true } pawn
            && pawn.Index == sourceEntityIndex);
        if (emitter is null)
        {
            message.Recipients.Clear();
            return HookResult.Continue;
        }

        foreach (var recipient in message.Recipients.ToArray())
        {
            var holder = Utilities.GetPlayerFromSlot(recipient.Slot);
            if (holder is not { IsValid: true, PawnIsAlive: true }
                || !_holders.Contains(holder.Index)
                || holder.Team == emitter.Team)
            {
                message.Recipients.Remove(recipient);
            }
        }

        return HookResult.Continue;
    }

    private double GetCooldownSeconds() => float.IsFinite(_settings.CooldownSeconds)
        ? Math.Max(0.1f, _settings.CooldownSeconds)
        : 2.0f;
}
