using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace Myrt1eSkill_Remake.Core;

public sealed class ThrowingKnifeService
{
    private readonly Myrt1eSkillRemakePlugin _plugin;
    private readonly ConcurrentDictionary<uint, Action<CBaseEntity>> _touchHandlers = new();
    private bool _loaded;

    public ThrowingKnifeService(Myrt1eSkillRemakePlugin plugin)
    {
        _plugin = plugin;
    }

    public void Load()
    {
        if (_loaded)
        {
            return;
        }

#pragma warning disable CS0618
        VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook(OnStartTouch, HookMode.Post);
#pragma warning restore CS0618
        _loaded = true;
    }

    public void Unload()
    {
        if (!_loaded)
        {
            return;
        }

        try
        {
#pragma warning disable CS0618
            VirtualFunctions.CBaseTrigger_StartTouchFunc.Unhook(OnStartTouch, HookMode.Post);
#pragma warning restore CS0618
        }
        catch (Exception exception)
        {
            _plugin.Logger.LogError(exception, "Failed to unhook throwing-knife touch detection");
        }
        finally
        {
            _touchHandlers.Clear();
            _loaded = false;
        }
    }

    public CTriggerMultiple? CreateTrigger(
        CBasePlayerWeapon knife,
        float radius,
        EffectScope effects,
        Action<CBaseEntity> onTouch)
    {
        var position = knife.AbsOrigin;
        if (!knife.IsValid || position is null)
        {
            return null;
        }

        var trigger = Utilities.CreateEntityByName<CTriggerMultiple>("trigger_multiple");
        if (trigger is null || !trigger.IsValid || trigger.AbsOrigin is null)
        {
            return null;
        }

        var safeRadius = float.IsFinite(radius) ? Math.Max(1.0f, radius) : 10.0f;
        trigger.Collision.SolidType = SolidType_t.SOLID_CAPSULE;
        trigger.Collision.SolidFlags = 1;
        trigger.Spawnflags = 1;
        trigger.Globalname = $"myrt1eskill_throwingknife_{trigger.Index}";
        trigger.AbsOrigin.X = position.X;
        trigger.AbsOrigin.Y = position.Y;
        trigger.AbsOrigin.Z = position.Z;
        trigger.Collision.CapsuleRadius = safeRadius;
        trigger.Collision.BoundingRadius = safeRadius;
        trigger.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_TRIGGER;
        trigger.Collision.EnablePhysics = 1;
        trigger.Collision.TriggerBloat = 0;
        trigger.Collision.SurroundType = SurroundingBoundsType_t.USE_OBB_COLLISION_BOUNDS;
        trigger.Collision.CollisionAttribute.CollisionFunctionMask = 39;
        trigger.Collision.CollisionAttribute.CollisionGroup = 2;
        trigger.DispatchSpawn();
        trigger.AcceptInput("SetParent", knife, knife, "!activator");

        _touchHandlers[trigger.Index] = onTouch;
        effects.TrackEntity(trigger);
        effects.RegisterCleanup(() => _touchHandlers.TryRemove(trigger.Index, out _));
        return trigger;
    }

    public void RemoveTrigger(CTriggerMultiple? trigger)
    {
        if (trigger is null)
        {
            return;
        }

        _touchHandlers.TryRemove(trigger.Index, out _);
        if (trigger.IsValid)
        {
            trigger.Remove();
        }
    }

    private HookResult OnStartTouch(DynamicHook hook)
    {
        var trigger = hook.GetParam<CBaseTrigger>(0);
        var entity = hook.GetParam<CBaseEntity>(1);
        if (trigger is { IsValid: true }
            && entity is { IsValid: true }
            && _touchHandlers.TryGetValue(trigger.Index, out var handler))
        {
            handler(entity);
        }

        return HookResult.Continue;
    }
}
