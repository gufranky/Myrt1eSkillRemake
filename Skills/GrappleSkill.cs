using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class GrappleSkill : ISkill, ITickSkill, IPlayerDeathSkill
{
    private sealed class GrappleState
    {
        public Vector? Anchor { get; set; }
        public int EndTick { get; set; }
        public CBeam? Rope { get; set; }
        public CDynamicProp? Hook { get; set; }
    }

    private readonly GrappleSettings _settings;
    private readonly GrappleService _grapple;

    public GrappleSkill(GrappleSettings settings, GrappleService grapple)
    {
        _settings = settings;
        _grapple = grapple;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Grapple",
        DisplayName = "🪝 抓钩",
        Description = "按 [css_useskill] 向瞄准点发射钩子，然后将自己拉过去。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Rare,
        DefaultWeight = 10,
        MaxPerServer = -1,
        CooldownSeconds = 10.0f,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "flight-control",
            "player-pull-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        context.State.Set(new GrappleState());
    }

    public void OnActivated(in SkillContext context)
    {
        var state = context.State.GetOrCreate<GrappleState>();
        DestroyVisuals(state);

        if (!_grapple.TryEyeTrace(context.Player, out var hit))
        {
            PluginText.Chat(context.Player, "[抓钩] 瞄准范围内没有可用的墙面锚点。");
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (pawn is not { IsValid: true } || origin is null)
        {
            return;
        }

        var eye = new Vector(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
        if (Distance(eye, hit.Position) < FinitePositiveOr(_settings.MinimumDistance, 150.0f))
        {
            PluginText.Chat(context.Player, "[抓钩] 锚点太近了。");
            return;
        }

        state.Anchor = hit.Position;
        state.EndTick = Server.TickCount + (int)MathF.Ceiling(
            FinitePositiveOr(_settings.MaximumPullSeconds, 3.0f) * 64.0f);
        state.Hook = CreateHook(context, hit);
        state.Rope = CreateRope(context, eye, hit.Position);
        (state.Hook as CBaseEntity ?? pawn).EmitSound(
            "SolidMetal.BulletImpact",
            volume: Math.Clamp(FiniteOr(_settings.SoundVolume, 1.0f), 0.0f, 1.0f));
    }

    public void OnTick(in SkillContext context)
    {
        if (!context.State.TryGet<GrappleState>(out var state) || state.Anchor is null)
        {
            return;
        }

        var pawn = context.Player.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (!context.Player.PawnIsAlive
            || pawn is not { IsValid: true }
            || origin is null
            || Server.TickCount >= state.EndTick)
        {
            StopPull(state);
            return;
        }

        var eye = new Vector(origin.X, origin.Y, origin.Z + pawn.ViewOffset.Z);
        var deltaX = state.Anchor.X - eye.X;
        var deltaY = state.Anchor.Y - eye.Y;
        var deltaZ = state.Anchor.Z - eye.Z;
        var distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        if (distance <= FinitePositiveOr(_settings.StopDistance, 90.0f))
        {
            StopPull(state);
            return;
        }

        var speed = FinitePositiveOr(_settings.PullSpeed, 850.0f);
        var scale = speed / distance;
        pawn.AbsVelocity.X = deltaX * scale;
        pawn.AbsVelocity.Y = deltaY * scale;
        pawn.AbsVelocity.Z = deltaZ * scale;

        if (state.Rope is { IsValid: true })
        {
            state.Rope.Teleport(eye);
        }
    }

    public void OnPlayerDeath(in SkillContext context, EventPlayerDeath @event)
    {
        if (@event.Userid?.Index == context.Player.Index
            && context.State.TryGet<GrappleState>(out var state))
        {
            StopPull(state);
        }
    }

    public void OnRevoked(in SkillContext context)
    {
        if (context.State.TryGet<GrappleState>(out var state))
        {
            StopPull(state);
        }
    }

    private CDynamicProp? CreateHook(in SkillContext context, GrappleService.TraceHit hit)
    {
        var hook = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (hook is not { IsValid: true })
        {
            return null;
        }

        var normalLength = MathF.Sqrt(
            hit.Normal.X * hit.Normal.X + hit.Normal.Y * hit.Normal.Y + hit.Normal.Z * hit.Normal.Z);
        if (normalLength < 0.0001f)
        {
            hook.Remove();
            return null;
        }

        var normalX = hit.Normal.X / normalLength;
        var normalY = hit.Normal.Y / normalLength;
        var normalZ = hit.Normal.Z / normalLength;
        var scale = Math.Max(0.01f, FiniteOr(_settings.HookScale, 0.4f));
        var embed = FiniteOr(_settings.HookEmbed, 8.0f) * scale;
        var position = new Vector(
            hit.Position.X + normalX * embed,
            hit.Position.Y + normalY * embed,
            hit.Position.Z + normalZ * embed);
        var angle = new QAngle(
            -MathF.Asin(Math.Clamp(-normalZ, -1.0f, 1.0f)) * 180.0f / MathF.PI,
            MathF.Atan2(-normalY, -normalX) * 180.0f / MathF.PI,
            0.0f);

        hook.SetModel(GrappleService.HookModel);
        hook.Teleport(position, angle, null);
        hook.DispatchSpawn();
        var skeleton = hook.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton is not null)
        {
            skeleton.Scale = scale;
            hook.AcceptInput("SetScale", null, null, scale.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        Utilities.SetStateChanged(hook, "CBaseEntity", "m_CBodyComponent");
        return context.Effects.TrackEntity(hook);
    }

    private CBeam? CreateRope(in SkillContext context, Vector start, Vector end)
    {
        var rope = Utilities.CreateEntityByName<CBeam>("env_beam");
        if (rope is not { IsValid: true })
        {
            return null;
        }

        rope.Render = context.Player.Team == CsTeam.Terrorist
            ? Color.FromArgb(255, 138, 84, 34)
            : Color.FromArgb(255, 52, 92, 138);
        var width = Math.Max(0.1f, FiniteOr(_settings.RopeWidth, 0.8f));
        rope.Width = width;
        rope.EndWidth = width;
        rope.Teleport(start);
        rope.EndPos.X = end.X;
        rope.EndPos.Y = end.Y;
        rope.EndPos.Z = end.Z;
        rope.DispatchSpawn();
        Utilities.SetStateChanged(rope, "CBeam", "m_fWidth");
        Utilities.SetStateChanged(rope, "CBeam", "m_fEndWidth");
        return context.Effects.TrackEntity(rope);
    }

    private static void StopPull(GrappleState state)
    {
        state.Anchor = null;
        state.EndTick = 0;
        DestroyVisuals(state);
    }

    private static void DestroyVisuals(GrappleState state)
    {
        if (state.Rope is { IsValid: true })
        {
            state.Rope.Remove();
        }
        if (state.Hook is { IsValid: true })
        {
            state.Hook.Remove();
        }
        state.Rope = null;
        state.Hook = null;
    }

    private static float Distance(Vector left, Vector right)
    {
        var x = left.X - right.X;
        var y = left.Y - right.Y;
        var z = left.Z - right.Z;
        return MathF.Sqrt(x * x + y * y + z * z);
    }

    private static float FinitePositiveOr(float value, float fallback) =>
        float.IsFinite(value) && value > 0.0f ? value : fallback;

    private static float FiniteOr(float value, float fallback) =>
        float.IsFinite(value) ? value : fallback;
}
