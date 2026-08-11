using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class HotBombSkill : ISkill, ITickSkill
{
    private sealed class HotBombState
    {
        public int NextDamageTick { get; set; }
        public bool Active { get; set; } = true;
    }

    private readonly HotBombSettings _settings;

    public HotBombSkill(HotBombSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "HotBomb",
        DisplayName = "🔥 热炸弹",
        Description = "只要你还活着，C4 就会持续对携带者造成伤害。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        MaxPerServer = 1,
        OnlyTeam = CsTeam.CounterTerrorist,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "c4-carrier-damage-control",
            "c4-render-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var intervalTicks = IntervalTicks(_settings.DamageIntervalSeconds);
        var state = new HotBombState
        {
            NextDamageTick = Server.TickCount + intervalTicks
        };
        context.State.Set(state);
        context.Effects.RegisterCleanup(() => state.Active = false);
        context.Effects.AddTimer(0.01f, () => SetBombColor(Color.Red));

        foreach (var player in Utilities.GetPlayers().Where(player =>
                     player.IsValid && player.PawnIsAlive && player.Team == CsTeam.Terrorist))
        {
            PluginText.Center(player, "🔥 C4 已经变热，携带它会持续受到伤害！");
        }
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
        if (context.State.TryGet<HotBombState>(out var state))
        {
            state.Active = false;
        }

        SetBombColor(Color.White);
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.Player.IsValid
            || !context.Player.PawnIsAlive
            || !context.State.TryGet<HotBombState>(out var state)
            || !state.Active
            || Server.TickCount < state.NextDamageTick
            || IsFreezePeriod())
        {
            return;
        }

        state.NextDamageTick = Server.TickCount + IntervalTicks(_settings.DamageIntervalSeconds);
        var damage = PositiveFiniteOr(_settings.Damage, 2.0f);
        var soundVolume = Math.Clamp(FiniteOr(_settings.SoundVolume, 0.35f), 0.0f, 1.0f);

        foreach (var carrier in Utilities.GetPlayers())
        {
            if (!carrier.IsValid
                || !carrier.PawnIsAlive
                || carrier.Team != CsTeam.Terrorist
                || !HasC4(carrier))
            {
                continue;
            }

            if (SkillDamage.TryDeal(context.Player, carrier, damage, DamageTypes_t.DMG_BURN))
            {
                carrier.PlayerPawn.Value?.EmitSound("Player.DamageBody.Victim", volume: soundVolume);
                PluginText.Center(carrier, $"🔥 C4 灼烧：-{damage:0.#} HP");
            }
        }
    }

    private static bool HasC4(CCSPlayerController player) =>
        player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_c4" }) == true;

    private static bool IsFreezePeriod() =>
        Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .FirstOrDefault()?.GameRules?.FreezePeriod == true;

    private static void SetBombColor(Color color)
    {
        foreach (var bomb in Utilities.FindAllEntitiesByDesignerName<CBasePlayerWeapon>("weapon_c4"))
        {
            if (!bomb.IsValid)
            {
                continue;
            }

            bomb.Render = color;
            Utilities.SetStateChanged(bomb, "CBaseModelEntity", "m_clrRender");
        }
    }

    private static int IntervalTicks(float seconds)
    {
        var safeSeconds = PositiveFiniteOr(seconds, 1.0f);
        return Math.Max(1, (int)MathF.Ceiling(safeSeconds * 64.0f));
    }

    private static float PositiveFiniteOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static float FiniteOr(float value, float fallback) =>
        float.IsFinite(value) ? value : fallback;
}
