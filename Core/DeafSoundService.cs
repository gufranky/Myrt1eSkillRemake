using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Removes deafened players from every server sound-event recipient list.
/// Owner tags make overlapping debuffs safe to release independently.
/// </summary>
public sealed class DeafSoundService : IDisposable
{
    public const int SoundEventMessageId = 208;

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly Dictionary<uint, HashSet<string>> _owners = new();
    private readonly HashSet<string> _globalOwners = new(StringComparer.Ordinal);
    private bool _loaded;

    public DeafSoundService(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
    }

    public void Load()
    {
        if (_loaded)
        {
            return;
        }

        _plugin.HookUserMessage(SoundEventMessageId, OnSoundEvent);
        _loaded = true;
    }

    public void Unload()
    {
        if (_loaded)
        {
            _plugin.UnhookUserMessage(SoundEventMessageId, OnSoundEvent);
            _loaded = false;
        }

        _owners.Clear();
        _globalOwners.Clear();
    }

    public bool Mute(CCSPlayerController player, string owner)
    {
        if (!player.IsValid || string.IsNullOrWhiteSpace(owner))
        {
            return false;
        }

        if (!_owners.TryGetValue(player.Index, out var owners))
        {
            owners = new HashSet<string>(StringComparer.Ordinal);
            _owners[player.Index] = owners;
        }

        owners.Add(owner);
        return true;
    }

    public bool Release(uint playerIndex, string owner)
    {
        if (!_owners.TryGetValue(playerIndex, out var owners) || !owners.Remove(owner))
        {
            return false;
        }

        if (owners.Count == 0)
        {
            _owners.Remove(playerIndex);
        }

        return true;
    }

    public void ClearPlayer(uint playerIndex) => _owners.Remove(playerIndex);

    public bool MuteAll(string owner) =>
        !string.IsNullOrWhiteSpace(owner) && _globalOwners.Add(owner);

    public bool ReleaseAll(string owner) => _globalOwners.Remove(owner);

    public void Dispose() => Unload();

    private HookResult OnSoundEvent(UserMessage message)
    {
        if (_globalOwners.Count > 0)
        {
            message.Recipients.Clear();
            return HookResult.Continue;
        }

        foreach (var playerIndex in _owners.Keys.ToArray())
        {
            var player = Utilities.GetPlayerFromIndex((int)playerIndex);
            if (player is not { IsValid: true })
            {
                _owners.Remove(playerIndex);
                continue;
            }

            message.Recipients.Remove(player);
        }

        return HookResult.Continue;
    }
}
