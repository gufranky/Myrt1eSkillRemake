using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Myrt1eSkill_Remake.Core;

public sealed class IlliterateService
{
    private readonly HashSet<uint> _holders = new();
    private int _offset = NextOffset();
    private int _lastChangeTick = int.MinValue;

    public void AddHolder(CCSPlayerController holder)
    {
        if (!holder.IsValid || !_holders.Add(holder.Index))
        {
            return;
        }

        foreach (var player in Utilities.GetPlayers().Where(IsAffected))
        {
            const string alert = "敌方的文盲诅咒正在干扰你阅读插件文字！";
            PluginText.Center(player, alert, bypass: true);
            PluginText.Chat(player, $"[文盲] {alert}", bypass: true);
        }
    }

    public void RemoveHolder(CCSPlayerController? holder)
    {
        if (holder is not null)
        {
            _holders.Remove(holder.Index);
        }
    }

    public string TransformFor(CCSPlayerController player, string input)
    {
        if (string.IsNullOrEmpty(input) || !IsAffected(player))
        {
            return input;
        }

        if (Server.TickCount - _lastChangeTick > 64 || _lastChangeTick == int.MinValue)
        {
            _offset = NextOffset();
            _lastChangeTick = Server.TickCount;
        }

        return Scramble(input, _offset);
    }

    public bool IsAffected(CCSPlayerController? player)
    {
        if (player is not { IsValid: true } || player.Team == CsTeam.Spectator)
        {
            return false;
        }

        foreach (var holderIndex in _holders.ToArray())
        {
            var holder = Utilities.GetPlayerFromIndex((int)holderIndex);
            if (holder is not { IsValid: true })
            {
                _holders.Remove(holderIndex);
                continue;
            }

            if (holder.Team == player.Team)
            {
                continue;
            }

            var pawn = holder.PlayerPawn.Value;
            if (pawn is { IsValid: true } && pawn.Health > 0)
            {
                return true;
            }
        }

        return false;
    }

    public static string Scramble(string input, int offset)
    {
        offset = ((offset % 26) + 26) % 26;
        var characters = input.Select(character =>
        {
            if (char.IsDigit(character))
            {
                return '?';
            }

            if (!char.IsLetter(character))
            {
                return character;
            }

            var baseCharacter = char.IsUpper(character) ? 'A' : 'a';
            var shifted = (character - baseCharacter + offset) % 26;
            if (shifted < 0)
            {
                shifted += 26;
            }

            return (char)(baseCharacter + shifted);
        }).ToArray();
        return new string(characters);
    }

    private static int NextOffset()
    {
        var offset = Random.Shared.Next(1, 26);
        return offset == 13 ? 14 : offset;
    }
}
