using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class FlashlightSkill : ISkill, ITickSkill, IPlayerDeathSkill
{
    private sealed class FlashlightState
    {
        public CBarnLight? Light { get; set; }
        public bool Enabled { get; set; }
    }

    private readonly FlashlightSettings _settings;
    private readonly AntiFlashSkill _antiFlash;

    public FlashlightSkill(FlashlightSettings settings, AntiFlashSkill antiFlash)
    {
        _settings = settings;
        _antiFlash = antiFlash;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Flashlight",
        DisplayName = "🔦 手电筒",
        Description = "点击 [css_useskill] 开关手电筒；光束会致盲正看向它的敌人。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Legendary,
        DefaultWeight = 10,
        MaxPerServer = 2,
        CooldownSeconds = 2.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-light-source"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new FlashlightState());
    }

    public void OnActivated(in SkillContext context)
    {
        var state = context.State.GetOrCreate<FlashlightState>();
        if (state.Light is not { IsValid: true })
        {
            state.Light = CreateLight(context);
            state.Enabled = state.Light is not null;
            if (!state.Enabled)
            {
                PluginText.Chat(context.Player, "[手电筒] 当前无法创建光源。");
            }
            return;
        }

        state.Enabled = !state.Enabled;
        state.Light.Enabled = state.Enabled;
        state.Light.AcceptInput(state.Enabled ? "TurnOn" : "TurnOff");
        if (state.Enabled)
        {
            UpdateLight(context.Player, state.Light);
        }
        else
        {
            state.Light.Teleport(new Vector(0.0f, 0.0f, -1000.0f));
        }
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.Player.IsValid || !context.Player.PawnIsAlive)
        {
            return;
        }

        var state = context.State.GetOrCreate<FlashlightState>();
        if (!state.Enabled || state.Light is not { IsValid: true })
        {
            return;
        }

        UpdateLight(context.Player, state.Light);
        if (Server.TickCount % 32 == 0)
        {
            BlindEnemies(context.Player);
        }
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid?.Index != context.Player.Index)
        {
            return;
        }

        var state = context.State.GetOrCreate<FlashlightState>();
        if (state.Light is { IsValid: true })
        {
            state.Light.Remove();
        }
        state.Light = null;
        state.Enabled = false;
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    private CBarnLight? CreateLight(in SkillContext context)
    {
        if (!context.Player.IsValid || !context.Player.PawnIsAlive)
        {
            return null;
        }

        var light = Utilities.CreateEntityByName<CBarnLight>("light_barn");
        if (light is not { IsValid: true })
        {
            return null;
        }

        light.Enabled = true;
        light.Color = Color.FromArgb(
            255,
            Math.Clamp(_settings.ColorR, 0, 255),
            Math.Clamp(_settings.ColorG, 0, 255),
            Math.Clamp(_settings.ColorB, 0, 255));
        light.ColorTemperature = 6500;
        light.Brightness = float.IsFinite(_settings.Brightness) ? Math.Max(0.0f, _settings.Brightness) : 1.5f;
        light.Range = float.IsFinite(_settings.Range) ? Math.Max(0.0f, _settings.Range) : 1200.0f;
        light.SoftX = 1.0f;
        light.SoftY = 1.0f;
        light.Skirt = 0.5f;
        light.SkirtNear = 1.0f;
        light.CastShadows = 0;
        light.DirectLight = 3;
        light.SizeParams.X = 45.0f;
        light.SizeParams.Y = 45.0f;
        light.SizeParams.Z = 0.03f;
        light.DispatchSpawn();
        light.AcceptInput("TurnOn");
        UpdateLight(context.Player, light);
        context.Effects.TrackEntity(light);
        return light;
    }

    private static void UpdateLight(CCSPlayerController player, CBarnLight light)
    {
        var pawn = player.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (pawn is not { IsValid: true } || origin is null || !light.IsValid)
        {
            return;
        }

        var eye = new Vector(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
        var forward = Forward(pawn.EyeAngles);
        var position = new Vector(
            eye.X + forward.X * 18.0f,
            eye.Y + forward.Y * 18.0f,
            eye.Z + forward.Z * 18.0f);
        light.Teleport(position, pawn.EyeAngles, null);
    }

    private void BlindEnemies(CCSPlayerController owner)
    {
        var ownerPawn = owner.PlayerPawn.Value;
        var ownerOrigin = ownerPawn?.AbsOrigin;
        if (ownerPawn is not { IsValid: true } || ownerOrigin is null)
        {
            return;
        }

        var range = float.IsFinite(_settings.Range) ? Math.Max(0.0f, _settings.Range) : 1200.0f;
        var rangeSquared = range * range;
        var angle = float.IsFinite(_settings.BlindAngle) ? Math.Clamp(_settings.BlindAngle, 0.0f, 180.0f) : 10.0f;
        var minimumDot = MathF.Cos(angle * MathF.PI / 180.0f);
        var ownerForward = Forward(ownerPawn.EyeAngles);
        var ownerEye = new Vector(ownerOrigin.X, ownerOrigin.Y, ownerOrigin.Z + ownerPawn.ViewOffset.Z);
        var lightPosition = new Vector(
            ownerEye.X + ownerForward.X * 18.0f,
            ownerEye.Y + ownerForward.Y * 18.0f,
            ownerEye.Z + ownerForward.Z * 18.0f);

        foreach (var target in Utilities.GetPlayers())
        {
            if (!target.IsValid
                || !target.PawnIsAlive
                || target.Index == owner.Index
                || target.TeamNum == owner.TeamNum
                || _antiFlash.IsHolder(target))
            {
                continue;
            }

            var targetPawn = target.PlayerPawn.Value;
            var targetOrigin = targetPawn?.AbsOrigin;
            if (targetPawn is not { IsValid: true } || targetOrigin is null)
            {
                continue;
            }

            var ownerBlinded = ownerPawn.BlindUntilTime > Server.CurrentTime;
            var targetBlinded = targetPawn.BlindUntilTime > Server.CurrentTime;
            var (ownerSeesTarget, targetSeesOwner) = AreSpottedBothWays(owner, ownerPawn, target, targetPawn);
            if ((ownerBlinded && targetBlinded)
                || (!ownerBlinded && !ownerSeesTarget)
                || (!targetBlinded && !targetSeesOwner))
            {
                continue;
            }

            var targetEye = new Vector(targetOrigin.X, targetOrigin.Y, targetOrigin.Z + targetPawn.ViewOffset.Z);
            var toTarget = Subtract(targetEye, lightPosition);
            var distanceSquared = LengthSquared(toTarget);
            if (distanceSquared <= 0.0001f || distanceSquared > rangeSquared)
            {
                continue;
            }

            var directionToTarget = Normalize(toTarget);
            if (Dot(ownerForward, directionToTarget) < minimumDot)
            {
                continue;
            }

            var directionToLight = Normalize(Subtract(lightPosition, targetEye));
            if (Dot(Forward(targetPawn.EyeAngles), directionToLight) < minimumDot)
            {
                continue;
            }

            ApplyBlind(targetPawn);
        }
    }

    private void ApplyBlind(CCSPlayerPawn pawn)
    {
        var duration = float.IsFinite(_settings.BlindDuration) ? Math.Max(0.0f, _settings.BlindDuration) : 5.0f;
        var alpha = float.IsFinite(_settings.BlindAlpha) ? Math.Clamp(_settings.BlindAlpha, 0.0f, 255.0f) : 200.0f;
        if (pawn.BlindUntilTime <= Server.CurrentTime)
        {
            pawn.BlindStartTime = Server.CurrentTime;
        }

        pawn.BlindUntilTime = Server.CurrentTime + duration;
        pawn.FlashDuration = duration + Random.Shared.NextSingle() * 0.05f;
        pawn.FlashMaxAlpha = alpha + Random.Shared.NextSingle() * 0.5f;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flFlashDuration");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flFlashMaxAlpha");
    }

    private static (bool OwnerSeesTarget, bool TargetSeesOwner) AreSpottedBothWays(
        CCSPlayerController owner,
        CCSPlayerPawn ownerPawn,
        CCSPlayerController target,
        CCSPlayerPawn targetPawn)
    {
        if (owner.Slot < 0 || target.Slot < 0)
        {
            return (false, false);
        }

        var ownerChunk = owner.Slot / 32;
        var targetChunk = target.Slot / 32;
        var ownerMask = 1u << (owner.Slot % 32);
        var targetMask = 1u << (target.Slot % 32);
        var ownerSeesTarget = (targetPawn.EntitySpottedState.SpottedByMask[ownerChunk] & ownerMask) != 0;
        var targetSeesOwner = (ownerPawn.EntitySpottedState.SpottedByMask[targetChunk] & targetMask) != 0;
        return (ownerSeesTarget, targetSeesOwner);
    }

    private static Vector Forward(QAngle angle)
    {
        var pitch = angle.X * MathF.PI / 180.0f;
        var yaw = angle.Y * MathF.PI / 180.0f;
        var cosinePitch = MathF.Cos(pitch);
        return new Vector(cosinePitch * MathF.Cos(yaw), cosinePitch * MathF.Sin(yaw), -MathF.Sin(pitch));
    }

    private static Vector Subtract(Vector left, Vector right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    private static float LengthSquared(Vector vector) =>
        vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;

    private static Vector Normalize(Vector vector)
    {
        var inverseLength = 1.0f / MathF.Sqrt(LengthSquared(vector));
        return new Vector(vector.X * inverseLength, vector.Y * inverseLength, vector.Z * inverseLength);
    }

    private static float Dot(Vector left, Vector right) =>
        left.X * right.X + left.Y * right.Y + left.Z * right.Z;
}
