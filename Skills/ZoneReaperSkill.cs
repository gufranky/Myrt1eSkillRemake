using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class ZoneReaperSkill : ISkill, ITickSkill
{
    private sealed class ZoneReaperState
    {
        public bool Used { get; set; }
        public bool Revoked { get; set; }
        public uint? DisabledTargetIndex { get; set; }
        public string? DisabledSite { get; set; }
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "ZoneReaper",
        DisplayName = "🚫 禁区收割者",
        Description = "选择一个包点，使敌人本回合无法在那里下包。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = 1,
        CooldownSeconds = 0.0f,
        OnlyTeam = CsTeam.CounterTerrorist,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bombsite-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new ZoneReaperState());
    }

    public void OnActivated(in SkillContext context)
    {
        if (!context.State.TryGet<ZoneReaperState>(out var state) || state.Revoked)
        {
            return;
        }

        if (state.Used)
        {
            PluginText.Chat(context.Player, $"[禁区收割者] 本回合已经关闭了 {state.DisabledSite} 包点。");
            return;
        }

        var bombTargets = FindBombTargets();
        if (bombTargets.Length != 2)
        {
            PluginText.Chat(context.Player, "[禁区收割者] 当前地图没有提供两个可识别的包点。");
            return;
        }

        var menu = new CenterHtmlMenu(
            PluginText.Transform(context.Player, "🚫 禁区收割者：选择要关闭的包点"),
            context.Plugin);
        AddSiteOption(menu, context.Player, state, "A", bombTargets[0].Index);
        AddSiteOption(menu, context.Player, state, "B", bombTargets[1].Index);
        MenuManager.OpenCenterHtmlMenu(context.Plugin, context.Player, menu);
    }

    public void OnRevoked(in SkillContext context)
    {
        if (!context.State.TryGet<ZoneReaperState>(out var state))
        {
            return;
        }

        state.Revoked = true;
        MenuManager.CloseActiveMenu(context.Player);
        RestoreBombsite(state);
    }

    public void OnTick(in SkillContext context)
    {
        if (Server.TickCount % 16 != 0
            || !context.State.TryGet<ZoneReaperState>(out var state)
            || state.Revoked
            || !state.Used)
        {
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive || player.Team != CsTeam.Terrorist)
            {
                continue;
            }

            var pawn = player.PlayerPawn.Value;
            var activeWeapon = pawn?.WeaponServices?.ActiveWeapon.Value;
            if (pawn is not { IsValid: true }
                || activeWeapon is not { IsValid: true }
                || !activeWeapon.DesignerName.Equals("weapon_c4", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!pawn.InBombZone && pawn.InBombZoneTrigger)
            {
                PluginText.Center(player, $"🚫 {state.DisabledSite} 包点已被关闭，无法在此下包！");
            }
        }
    }

    private static void AddSiteOption(
        CenterHtmlMenu menu,
        CCSPlayerController owner,
        ZoneReaperState state,
        string site,
        uint targetIndex)
    {
        menu.AddMenuOption(
            PluginText.Transform(owner, $"关闭 {site} 包点"),
            (player, option) => TryDisableBombsite(player, state, site, targetIndex));
    }

    private static void TryDisableBombsite(
        CCSPlayerController owner,
        ZoneReaperState state,
        string site,
        uint targetIndex)
    {
        MenuManager.CloseActiveMenu(owner);
        if (state.Revoked
            || state.Used
            || !owner.IsValid
            || !owner.PawnIsAlive
            || owner.Team != CsTeam.CounterTerrorist)
        {
            return;
        }

        var target = Utilities.GetEntityFromIndex<CBombTarget>((int)targetIndex);
        if (target is not { IsValid: true })
        {
            PluginText.Chat(owner, "[禁区收割者] 所选包点已经失效，请重新选择。");
            return;
        }

        target.BombPlantedHere = true;
        Utilities.SetStateChanged(target, "CBombTarget", "m_bBombPlantedHere");
        state.Used = true;
        state.DisabledTargetIndex = target.Index;
        state.DisabledSite = site;

        foreach (var teammate in Utilities.GetPlayers().Where(player =>
                     player.IsValid && player.Team == CsTeam.CounterTerrorist))
        {
            PluginText.Chat(teammate, $"[禁区收割者] 🚫 {owner.PlayerName} 已关闭 {site} 包点！");
        }
    }

    private static void RestoreBombsite(ZoneReaperState state)
    {
        if (state.DisabledTargetIndex is not { } targetIndex)
        {
            return;
        }

        var target = Utilities.GetEntityFromIndex<CBombTarget>((int)targetIndex);
        if (target is { IsValid: true })
        {
            target.BombPlantedHere = false;
            Utilities.SetStateChanged(target, "CBombTarget", "m_bBombPlantedHere");
        }

        state.DisabledTargetIndex = null;
        state.DisabledSite = null;
        state.Used = false;
    }

    private static CBombTarget[] FindBombTargets() =>
        Utilities.FindAllEntitiesByDesignerName<CBombTarget>("func_bomb_target")
            .Where(target => target is { IsValid: true })
            .OrderBy(target => target.Index)
            .ToArray();
}
