using System.Net;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Shows the round event and assigned skills using jRandomSkills-compatible
/// visibility defaults: persistent names and descriptions for seven seconds.
/// </summary>
public sealed class RoundPresentationService
{
    // Keep the presentation in sync with the server's 60 Hz update cadence.
    // The HUD is still skipped while a WASD menu is open and only active rounds
    // are rendered, so this does not create a permanent background stream.
    public static readonly TimeSpan HudUpdateInterval = TimeSpan.FromSeconds(1.0 / 60.0);

    private sealed record StatusLine(string Text, string Color);

    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly SkillManager _skills;
    private RoundPlan? _plan;
    private DateTime _hudExpiresAt;
    private DateTime _descriptionExpiresAt;
    private DateTime _nextHudUpdateAt;
    private long _generation;
    private readonly Dictionary<uint, Dictionary<string, StatusLine>> _statusLines = new();

    public RoundPresentationService(Myrt1eSkillRemakePlugin plugin, SkillManager skills)
    {
        _plugin = plugin;
        _skills = skills;
    }

    public void Reveal(RoundPlan plan)
    {
        var generation = ++_generation;
        _plan = plan;
        var now = DateTime.UtcNow;
        _hudExpiresAt = Expiry(now, _plugin.Config.SkillHudDuration);
        _descriptionExpiresAt = Expiry(now, _plugin.Config.SkillDescriptionDuration);
        _nextHudUpdateAt = DateTime.MinValue;

        var players = EligiblePlayers().ToArray();
        foreach (var player in players)
        {
            AnnounceOwnSkills(player);
        }

        if (_plugin.Config.TeamMateSkillChatInfo)
        {
            _plugin.AddTimer(0.4f, () =>
            {
                if (generation == _generation)
                {
                    AnnounceTeammates(players);
                }
            });
        }
    }

    public void Clear()
    {
        _generation++;
        _plan = null;
        _hudExpiresAt = DateTime.MinValue;
        _descriptionExpiresAt = DateTime.MinValue;
        _nextHudUpdateAt = DateTime.MinValue;
        _statusLines.Clear();
    }

    public void SetStatusLine(
        CCSPlayerController player,
        string sourceId,
        string text,
        string color = "#FFFFFF")
    {
        if (!player.IsValid || string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (!_statusLines.TryGetValue(player.Index, out var lines))
        {
            lines = new Dictionary<string, StatusLine>(StringComparer.OrdinalIgnoreCase);
            _statusLines[player.Index] = lines;
        }

        var next = new StatusLine(text, NormalizeColor(color));
        if (lines.TryGetValue(sourceId, out var current) && current == next)
        {
            return;
        }

        lines[sourceId] = next;
        _nextHudUpdateAt = DateTime.MinValue;
    }

    public void RemoveStatusLine(CCSPlayerController player, string sourceId)
    {
        if (!_statusLines.TryGetValue(player.Index, out var lines))
        {
            return;
        }

        lines.Remove(sourceId);
        _nextHudUpdateAt = DateTime.MinValue;
        if (lines.Count == 0)
        {
            _statusLines.Remove(player.Index);
        }
    }

    public void OnTick()
    {
        var plan = _plan;
        var now = DateTime.UtcNow;
        if (plan is null || now > _hudExpiresAt)
        {
            return;
        }

        // Refresh at 60 Hz while the round HUD is active.
        if (now < _nextHudUpdateAt)
        {
            return;
        }

        _nextHudUpdateAt = now + HudUpdateInterval;

        var showDescriptions = now <= _descriptionExpiresAt;
        foreach (var player in EligiblePlayers())
        {
            if (_plugin.WasdMenus.HasMenu(player))
            {
                continue;
            }

            player.PrintToCenterHtml(BuildHud(player, plan, showDescriptions));
        }
    }

    private void AnnounceOwnSkills(CCSPlayerController player)
    {
        if (!_plugin.Config.YourSkillChatInfo)
        {
            return;
        }

        var assigned = _skills.GetAssignedSkills(player);
        if (assigned.Count == 0)
        {
            PluginText.Chat(player, "[随机技能] 本回合没有技能。");
            return;
        }

        foreach (var skill in assigned)
        {
            PluginText.Chat(player, $"[随机技能] {skill.DisplayName}：{skill.Description}");
        }
    }

    private void AnnounceTeammates(IReadOnlyCollection<CCSPlayerController> originalPlayers)
    {
        foreach (var player in originalPlayers.Where(IsEligiblePlayer))
        {
            var lines = originalPlayers
                .Where(teammate => teammate.Slot != player.Slot && teammate.Team == player.Team)
                .Where(IsEligiblePlayer)
                .Select(teammate =>
                {
                    var names = _skills.GetAssignedSkills(teammate).Select(skill => skill.DisplayName).ToArray();
                    return names.Length == 0 ? null : $"{teammate.PlayerName}：{string.Join(" / ", names)}";
                })
                .Where(line => line is not null)
                .ToArray();

            if (lines.Length > 0)
            {
                PluginText.Chat(player, $"[队友技能] {string.Join("；", lines)}");
            }
        }
    }

    private string BuildHud(CCSPlayerController player, RoundPlan plan, bool showDescriptions)
    {
        var events = plan.Events.Count == 0
            ? "无"
            : string.Join(" / ", plan.Events.Select(item => item.DisplayName));
        var assigned = _skills.GetAssignedSkills(player);
        var skillNames = assigned.Count == 0
            ? "无"
            : string.Join(" / ", assigned.Select(item => item.DisplayName));

        var eventText = Encode(PluginText.Transform(player, $"当前事件：{events}"));
        var skillText = Encode(PluginText.Transform(player, $"你的技能：{skillNames}"));
        var html = $"<font color='#F4C95D'>{eventText}</font><br><font color='#8BE9FD'>{skillText}</font>";

        if (showDescriptions)
        {
            var eventDescriptions = string.Join("；", plan.Events.Select(item => item.Description));
            if (!string.IsNullOrWhiteSpace(eventDescriptions))
            {
                html += $"<br><font color='#D7D7D7'>{Encode(PluginText.Transform(player, eventDescriptions))}</font>";
            }

            var descriptions = assigned.Count == 0
                ? Encode(PluginText.Transform(player, "本回合没有技能。"))
                : string.Join("<br>", assigned.Select(item =>
                    Encode(PluginText.Transform(player, $"{item.DisplayName}：{item.Description}"))));
            html += $"<br><font color='#FFFFFF'>{descriptions}</font>";
        }

        if (_statusLines.TryGetValue(player.Index, out var statusLines))
        {
            foreach (var line in statusLines.Values)
            {
                html += $"<br><font color='{line.Color}'>{Encode(PluginText.Transform(player, line.Text))}</font>";
            }
        }

        return html;
    }

    private static string NormalizeColor(string color)
    {
        if (color.Length == 7
            && color[0] == '#'
            && color.Skip(1).All(character => char.IsAsciiHexDigit(character)))
        {
            return color;
        }

        return "#FFFFFF";
    }

    private static DateTime Expiry(DateTime now, float durationSeconds) =>
        durationSeconds < 0 ? DateTime.MaxValue : now.AddSeconds(Math.Max(0, durationSeconds));

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static IEnumerable<CCSPlayerController> EligiblePlayers() =>
        Utilities.GetPlayers().Where(IsEligiblePlayer);

    private static bool IsEligiblePlayer(CCSPlayerController player) =>
        player.IsValid
        && !player.IsHLTV
        && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist;
}
