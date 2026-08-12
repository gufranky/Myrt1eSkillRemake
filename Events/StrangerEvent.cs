using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class StrangerEvent : RoundEventBase, IRoundEventPreDamage
{
    private readonly NavMeshService _navMesh;
    private readonly Dictionary<int, string> _originalModels = new();
    private bool _active;
    private bool _reflectingFriendlyFire;
    private string _sharedModel = string.Empty;

    public StrangerEvent(NavMeshService navMesh)
    {
        _navMesh = navMesh;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "Stranger",
        DisplayName = "❓ 不认识的人",
        Description = "所有人随机使用同一个人的模型，出生位置随机；关闭雷达，攻击队友会伤害自己。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-model-rules",
            "player-spawn-rules",
            "radar-rules",
            "friendly-fire-damage-transform"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-model-control",
            "player-teleport-control",
            "friendly-fire-damage-transform"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        _reflectingFriendlyFire = false;
        _sharedModel = SelectSharedModel();
        context.Effects.RegisterCleanup(() =>
        {
            _active = false;
            RestoreModels();
            _sharedModel = string.Empty;
        });

        ConVarOverrides.Set(context.Effects, "sv_disable_radar", true);
        foreach (var player in Utilities.GetPlayers())
        {
            ApplyPlayer(player, teleport: true);
        }

        PrintToChatAll("[娱乐事件] ❓ 不认识的人：所有人都换成了同一个人的模型，雷达已关闭；攻击队友会伤害自己！");
    }

    public void OnBeforeDamage(
        in RoundEventContext context,
        CCSPlayerController victim,
        CCSPlayerController attacker,
        CTakeDamageInfo damageInfo)
    {
        if (!_active
            || _reflectingFriendlyFire
            || damageInfo.Damage <= 0.0f
            || attacker.Slot == victim.Slot
            || attacker.Team != victim.Team
            || !attacker.PawnIsAlive)
        {
            return;
        }

        var damage = damageInfo.Damage;
        damageInfo.Damage = 0.0f;
        damageInfo.TotalledDamage = 0.0f;
        damageInfo.ShouldBleed = false;
        _reflectingFriendlyFire = true;
        try
        {
            SkillDamage.TryDeal(attacker, attacker, damage, DamageTypes_t.DMG_GENERIC);
        }
        finally
        {
            _reflectingFriendlyFire = false;
        }
    }

    private void ApplyPlayer(CCSPlayerController? player, bool teleport)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        if (!_originalModels.ContainsKey(player.Slot))
        {
            _originalModels[player.Slot] = GetModelName(pawn);
        }

        if (!string.IsNullOrWhiteSpace(_sharedModel))
        {
            pawn.SetModel(_sharedModel);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_nModelIndex");
        }

        if (teleport)
        {
            _navMesh.TryTeleportRandom(player, out _);
        }
    }

    private string SelectSharedModel()
    {
        var models = Utilities.GetPlayers()
            .Where(player => player is { IsValid: true, PawnIsAlive: true })
            .Select(player => player.PlayerPawn.Value)
            .Where(pawn => pawn is { IsValid: true })
            .Select(pawn => GetModelName(pawn!))
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return models.Length == 0 ? string.Empty : models[Random.Shared.Next(models.Length)];
    }

    private void RestoreModels()
    {
        foreach (var (slot, model) in _originalModels)
        {
            var player = Utilities.GetPlayerFromSlot(slot);
            var pawn = player?.PlayerPawn.Value;
            if (pawn is not { IsValid: true } || string.IsNullOrWhiteSpace(model))
            {
                continue;
            }

            pawn.SetModel(model);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_nModelIndex");
        }

        _originalModels.Clear();
    }

    private static string GetModelName(CCSPlayerPawn pawn) =>
        pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState?.ModelName ?? string.Empty;
}
