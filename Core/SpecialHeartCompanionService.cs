using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;

namespace Myrt1eSkill_Remake.Core;

public sealed class SpecialHeartCompanionService : IDisposable
{
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly SpecialHeartSettings _settings;
    private readonly Dictionary<uint, uint> _companions = new();
    private readonly Dictionary<uint, ChickenMovementBoost.State> _movement = new();
    private bool _loaded;

    public SpecialHeartCompanionService(Myrt1eSkillRemakePlugin plugin, SpecialHeartSettings settings)
    {
        _plugin = plugin;
        _settings = settings;
    }

    public void Load()
    {
        if (_loaded) return;
        _plugin.RegisterListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
        _loaded = true;
    }

    public bool Spawn(CCSPlayerController owner)
    {
        Remove(owner.Index);
        var pawn = owner.PlayerPawn.Value;
        var origin = pawn?.AbsOrigin;
        if (owner is not { IsValid: true, PawnIsAlive: true } || pawn is not { IsValid: true } || origin is null)
        {
            return false;
        }

        var chicken = Utilities.CreateEntityByName<CChicken>("chicken");
        if (chicken is null) return false;
        chicken.DispatchSpawn();
        var health = Math.Clamp(_settings.ChickenHealth, 1, 10000);
        chicken.Render = Color.FromArgb(255, 80, 160, 255);
        chicken.MaxHealth = health;
        chicken.Health = health;
        SetScale(chicken, 3.0f);
        // The heart is a companion, not an escort: it should occupy the
        // owner's exact position from spawn onward.
        chicken.Teleport(
            new Vector(origin.X, origin.Y, origin.Z),
            pawn.AbsRotation,
            new Vector(0, 0, 0));
        chicken.Leader.Raw = owner.PlayerPawn.Raw;
        Utilities.SetStateChanged(chicken, "CBaseModelEntity", "m_clrRender");
        Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iMaxHealth");
        Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iHealth");
        _companions[owner.Index] = chicken.Index;
        _movement[owner.Index] = new ChickenMovementBoost.State();
        return true;
    }

    public bool Update(CCSPlayerController owner)
    {
        if (!_companions.TryGetValue(owner.Index, out var chickenIndex)) return true;
        var pawn = owner.PlayerPawn.Value;
        var chicken = Utilities.GetEntityFromIndex<CChicken>((int)chickenIndex);
        if (owner is not { IsValid: true, PawnIsAlive: true } || pawn is not { IsValid: true })
        {
            Remove(owner.Index);
            return true;
        }

        if (chicken is not { IsValid: true } || chicken.Health <= 0)
        {
            Remove(owner.Index);
            return false;
        }

        var origin = pawn.AbsOrigin;
        if (origin is null) return true;
        if (chicken.Leader.Raw != owner.PlayerPawn.Raw) chicken.Leader.Raw = owner.PlayerPawn.Raw;
        ChickenMovementBoost.Update(
            chicken,
            origin,
            _movement[owner.Index],
            PositiveOr(_settings.SpeedMultiplier, 3.0f),
            PositiveOr(_settings.MaximumExtraStep, 18.0f),
            teleportDirectlyToTarget: true);
        return true;
    }

    public void Remove(uint ownerIndex)
    {
        if (_companions.Remove(ownerIndex, out var index))
        {
            var chicken = Utilities.GetEntityFromIndex<CChicken>((int)index);
            if (chicken is { IsValid: true }) chicken.Remove();
        }
        _movement.Remove(ownerIndex);
    }

    public void Dispose()
    {
        foreach (var owner in _companions.Keys.ToArray()) Remove(owner);
        if (_loaded)
        {
            _plugin.RemoveListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
            _loaded = false;
        }
    }

    private HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo damageInfo)
    {
        if (entity is not { IsValid: true } || !_companions.ContainsValue(entity.Index)) return HookResult.Continue;
        var ownerIndex = _companions.First(pair => pair.Value == entity.Index).Key;
        var owner = Utilities.GetPlayerFromIndex((int)ownerIndex);
        var attacker = damageInfo.Attacker?.Value?.As<CCSPlayerPawn>();
        var isEnemyPlayer = owner is { IsValid: true }
            && attacker is { IsValid: true }
            && string.Equals(attacker.DesignerName, "player", StringComparison.Ordinal)
            && attacker.TeamNum != owner.TeamNum;
        if (!isEnemyPlayer)
        {
            damageInfo.Damage = 0.0f;
            damageInfo.TotalledDamage = 0.0f;
            damageInfo.ShouldBleed = false;
        }
        return HookResult.Continue;
    }

    private static void SetScale(CChicken chicken, float scale)
    {
        var skeleton = chicken.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton is not null) skeleton.Scale = scale;
        chicken.AcceptInput("SetScale", chicken, chicken, scale.ToString(CultureInfo.InvariantCulture));
        Utilities.SetStateChanged(chicken, "CBaseEntity", "m_CBodyComponent");
    }

    private static float PositiveOr(float value, float fallback) => float.IsFinite(value) && value > 0 ? value : fallback;
}
