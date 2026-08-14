using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class BlastShotSkill : ISkill, ITickSkill
{
    public const string RequiredWeapon = "weapon_mp5sd";

    private sealed class BlastShotState
    {
        public DateTime CooldownEndsAt { get; set; } = DateTime.MinValue;
        public bool Active { get; set; } = true;
    }

    private readonly BlastShotSettings _settings;
    private readonly ExplosiveProjectileService _explosions;

    public BlastShotSkill(
        BlastShotSettings settings,
        ExplosiveProjectileService explosions)
    {
        _settings = settings;
        _explosions = explosions;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "BlastShot",
        DisplayName = "💥 爆破射击",
        Description = "手持 MP5-SD 时按右键发射一枚 HE 手榴弹。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = 10.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-secondary-fire-control",
            "projectile-launcher-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var state = new BlastShotState();
        context.State.Set(state);
        var player = context.Player;
        var presentation = context.Plugin.RuntimePresentation;
        context.Effects.RegisterCleanup(() =>
        {
            state.Active = false;
            if (player.IsValid)
            {
                presentation.RemoveStatusLine(player, Descriptor.Id);
            }
        });

        GiveMp5(player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    private void TryFire(in SkillContext context, BlastShotState state)
    {
        var player = context.Player;
        if (!player.Buttons.HasFlag(PlayerButtons.Attack2))
        {
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true } || pawn.AbsOrigin is null)
        {
            return;
        }

        var activeWeapon = pawn.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon is not { IsValid: true }
            || !activeWeapon.DesignerName.Equals(RequiredWeapon, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (state.CooldownEndsAt > now)
        {
            var remaining = (state.CooldownEndsAt - now).TotalSeconds;
            PluginText.Center(player, $"爆破射击冷却中：{remaining:F1} 秒");
            return;
        }

        var force = FiniteOr(_settings.Force, 1000.0f, 0.0f);
        var forward = GetForwardVector(pawn.EyeAngles);
        var position = new Vector(
            pawn.AbsOrigin.X,
            pawn.AbsOrigin.Y,
            pawn.AbsOrigin.Z + pawn.ViewOffset.Z);
        var velocity = forward * force;
        var damage = FiniteOr(_settings.ExplosionDamage, 60.0f, 0.0f);
        var radius = FiniteOr(_settings.ExplosionRadius, 400.0f, 0.0f);
        var teammateMultiplier = float.IsFinite(_settings.TeammateDamageMultiplier)
            ? Math.Clamp(_settings.TeammateDamageMultiplier, 0.0f, 1.0f)
            : 0.50f;
        if (!_explosions.TryLaunchHe(
                position,
                velocity,
                player,
                damage,
                radius,
                teammateMultiplier))
        {
            PluginText.Chat(player, "[爆破射击] HE 投射物生成失败。");
            return;
        }

        var cooldown = FiniteOr(_settings.CooldownSeconds, 10.0f, 0.0f);
        state.CooldownEndsAt = now.AddSeconds(cooldown);
        PluginText.Center(player, "💥 HE 发射！");
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<BlastShotState>(out var state) || !state.Active)
        {
            return;
        }

        // CS2 does not reliably report Attack2 through OnPlayerButtonsChanged.
        // Match jRandomSkills by polling the authoritative button state instead.
        TryFire(context, state);

        var remaining = (state.CooldownEndsAt - DateTime.UtcNow).TotalSeconds;
        if (remaining <= 0.0)
        {
            context.Plugin.RuntimePresentation.RemoveStatusLine(context.Player, Descriptor.Id);
            return;
        }

        context.Plugin.RuntimePresentation.SetStatusLine(
            context.Player,
            Descriptor.Id,
            $"爆破射击冷却：{Math.Ceiling(remaining):0} 秒",
            "#FF5555");
    }

    private static void GiveMp5(CCSPlayerController player)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return;
        }

        var hasMp5 = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            handle => handle.Value is { IsValid: true } weapon
                      && weapon.DesignerName.Equals(RequiredWeapon, StringComparison.OrdinalIgnoreCase)) == true;
        if (!hasMp5)
        {
            player.GiveNamedItem(RequiredWeapon);
        }
    }

    private static Vector GetForwardVector(QAngle angles)
    {
        var pitch = -angles.X * (MathF.PI / 180.0f);
        var yaw = angles.Y * (MathF.PI / 180.0f);
        return new Vector(
            MathF.Cos(pitch) * MathF.Cos(yaw),
            MathF.Cos(pitch) * MathF.Sin(yaw),
            MathF.Sin(pitch));
    }

    private static float FiniteOr(float value, float fallback, float minimum) =>
        Math.Max(minimum, float.IsFinite(value) ? value : fallback);
}
