using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class DwarfSkill : ISkill
{
    private const float DefaultMinimumScale = 0.60f;
    private const float DefaultMaximumScale = 0.95f;

    private readonly DwarfSettings _settings;

    public DwarfSkill(DwarfSettings settings)
    {
        _settings = settings;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "Dwarf",
        DisplayName = "小矮人",
        Description = "回合开始时随机获得 60%～95% 的角色体型。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-scale-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        var pawn = context.Player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid)
        {
            return;
        }

        var configuredMinimum = float.IsFinite(_settings.MinimumScale)
            ? _settings.MinimumScale
            : DefaultMinimumScale;
        var configuredMaximum = float.IsFinite(_settings.MaximumScale)
            ? _settings.MaximumScale
            : DefaultMaximumScale;
        var minimum = Math.Clamp(Math.Min(configuredMinimum, configuredMaximum), 0.01f, 10.0f);
        var maximum = Math.Clamp(Math.Max(configuredMinimum, configuredMaximum), minimum, 10.0f);
        var scale = MathF.Round(minimum + Random.Shared.NextSingle() * (maximum - minimum), 2);
        var originalScale = GetScale(pawn);

        SetScale(pawn, scale);
        context.Effects.RegisterCleanup(() => SetScale(pawn, originalScale));
        PluginText.Chat(context.Player, $"[随机技能] 小矮人：你的体型倍率是 {scale:0.00}x");
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    private static float GetScale(CCSPlayerPawn pawn)
    {
        return pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.Scale ?? 1.0f;
    }

    private static void SetScale(CCSPlayerPawn pawn, float scale)
    {
        if (!pawn.IsValid || scale <= 0.0f)
        {
            return;
        }

        var skeleton = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton is null)
        {
            return;
        }

        skeleton.Scale = scale;
        pawn.AcceptInput("SetScale", null, null, scale.ToString(CultureInfo.InvariantCulture));
        Server.NextWorldUpdate(() =>
        {
            if (pawn.IsValid)
            {
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
            }
        });
    }
}
