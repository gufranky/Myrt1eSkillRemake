using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Single output boundary for plugin-owned player-facing text. Commands and
/// engine messages intentionally remain outside this boundary.
/// </summary>
public static class PluginText
{
    private static IlliterateService? _illiterate;

    public static void Configure(IlliterateService illiterate) => _illiterate = illiterate;

    public static void Reset() => _illiterate = null;

    public static string Transform(CCSPlayerController player, string message, bool bypass = false) =>
        !bypass && _illiterate is not null
            ? _illiterate.TransformFor(player, message)
            : message;

    public static void Chat(CCSPlayerController player, string message, bool bypass = false)
    {
        if (player.IsValid)
        {
            player.PrintToChat(Transform(player, message, bypass));
        }
    }

    public static void Center(CCSPlayerController player, string message, bool bypass = false)
    {
        if (player.IsValid)
        {
            player.PrintToCenter(Transform(player, message, bypass));
        }
    }

    public static void ChatAll(string message, bool bypass = false)
    {
        foreach (var player in Utilities.GetPlayers().Where(player => player.IsValid))
        {
            Chat(player, message, bypass);
        }
    }
}
