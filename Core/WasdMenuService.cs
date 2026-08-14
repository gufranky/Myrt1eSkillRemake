using System.Net;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace Myrt1eSkill_Remake.Core;

public sealed record WasdMenuOption(string Text, Action<CCSPlayerController, WasdMenuOption> OnSelected);

/// <summary>
/// Lightweight W/S/E center-HTML menu matching jRandomSkills' WASD menu UX.
/// </summary>
public sealed class WasdMenu
{
    private readonly List<WasdMenuOption> _options = new();

    public WasdMenu(string title, Myrt1eSkillRemakePlugin? plugin = null)
    {
        Title = title;
    }

    public string Title { get; }
    public IReadOnlyList<WasdMenuOption> Options => _options;

    public void AddMenuOption(
        string text,
        Action<CCSPlayerController, WasdMenuOption> onSelected) =>
        _options.Add(new WasdMenuOption(text, onSelected));
}

public sealed class WasdMenuService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);
    // Center HTML is much shorter than a normal panel. Long skill descriptions
    // wrap, so three logical options could occupy six or more visual lines and
    // clip the footer off-screen. Keep every page deliberately compact.
    private const int VisibleOptions = 2;
    private const int MaximumTitleCharacters = 24;
    private const int MaximumOptionCharacters = 32;

    private sealed class ActiveMenu
    {
        public required CCSPlayerController Player { get; init; }
        public required WasdMenu Menu { get; init; }
        public int SelectedIndex { get; set; }
        public DateTime NextRenderAt { get; set; }
    }

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly Dictionary<uint, ActiveMenu> _activeMenus = new();

    public WasdMenuService(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
    }

    public bool HasMenu(CCSPlayerController player) =>
        player.IsValid && _activeMenus.ContainsKey(player.Index);

    public void Open(CCSPlayerController player, WasdMenu menu)
    {
        if (!player.IsValid || menu.Options.Count == 0)
        {
            return;
        }

        var active = new ActiveMenu
        {
            Player = player,
            Menu = menu,
            NextRenderAt = DateTime.MinValue
        };
        _activeMenus[player.Index] = active;
        Render(active, DateTime.UtcNow);
    }

    public void Close(CCSPlayerController? player)
    {
        if (player is not null)
        {
            _activeMenus.Remove(player.Index);
        }
    }

    public void CloseAll() => _activeMenus.Clear();

    /// <summary>Returns true when an open menu consumed W, S, or E.</summary>
    public bool HandleButtons(CCSPlayerController player, PlayerButtons pressed)
    {
        if (!_activeMenus.TryGetValue(player.Index, out var active)
            || !player.IsValid)
        {
            return false;
        }

        if (pressed.HasFlag(PlayerButtons.Forward))
        {
            active.SelectedIndex = MoveSelection(active.SelectedIndex, active.Menu.Options.Count, -1);
            Render(active, DateTime.UtcNow);
            return true;
        }

        if (pressed.HasFlag(PlayerButtons.Back))
        {
            active.SelectedIndex = MoveSelection(active.SelectedIndex, active.Menu.Options.Count, 1);
            Render(active, DateTime.UtcNow);
            return true;
        }

        if (!pressed.HasFlag(PlayerButtons.Use))
        {
            return false;
        }

        var option = active.Menu.Options[active.SelectedIndex];
        _activeMenus.Remove(player.Index);
        try
        {
            option.OnSelected(player, option);
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(exception, "WASD menu callback failed for slot {Slot}", player.Slot);
        }

        return true;
    }

    public void OnTick()
    {
        var now = DateTime.UtcNow;
        foreach (var active in _activeMenus.Values.ToArray())
        {
            if (!active.Player.IsValid || !active.Player.PawnIsAlive)
            {
                _activeMenus.Remove(active.Player.Index);
                continue;
            }

            if (now >= active.NextRenderAt)
            {
                Render(active, now);
            }
        }
    }

    public static int MoveSelection(int currentIndex, int optionCount, int delta)
    {
        if (optionCount <= 0)
        {
            return 0;
        }

        return ((currentIndex + delta) % optionCount + optionCount) % optionCount;
    }

    private static string BuildHtml(ActiveMenu active)
    {
        var count = active.Menu.Options.Count;
        var start = Math.Clamp(active.SelectedIndex - 1, 0, Math.Max(0, count - VisibleOptions));
        var end = Math.Min(count, start + VisibleOptions);
        var title = ClampDisplayText(active.Menu.Title, MaximumTitleCharacters);
        var html = $"<font class='fontWeight-Bold fontSize-m' color='#FFD166'>★ {Encode(title)} ({active.SelectedIndex + 1}/{count}) ★</font><br>";

        for (var index = start; index < end; index++)
        {
            var text = Encode(ClampDisplayText(active.Menu.Options[index].Text, MaximumOptionCharacters));
            html += index == active.SelectedIndex
                ? $"<font class='fontSize-s' color='#B388FF'>[ </font><font class='fontSize-s' color='#A3FF8F'>{text}</font><font class='fontSize-s' color='#B388FF'> ]</font><br>"
                : $"<font class='fontSize-s' color='#E8E8E8'>{text}</font><br>";
        }

        return html
               + "<br><font class='fontSize-s' color='#9AA4B2'>W/S 选择</font>"
               + "　<font class='fontSize-s' color='#A3FF8F'>E 确认</font>";
    }

    public static string ClampDisplayText(string? value, int maximumCharacters)
    {
        if (string.IsNullOrWhiteSpace(value) || maximumCharacters <= 0)
        {
            return string.Empty;
        }

        var compact = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return compact.Length <= maximumCharacters
            ? compact
            : string.Concat(compact.AsSpan(0, Math.Max(1, maximumCharacters - 1)), "…");
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static void Render(ActiveMenu active, DateTime now)
    {
        active.Player.PrintToCenterHtml(BuildHtml(active));
        active.NextRenderAt = now + RefreshInterval;
    }
}
