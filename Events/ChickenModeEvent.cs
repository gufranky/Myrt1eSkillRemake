using System.Drawing;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class ChickenModeEvent : RoundEventBase,
    IRoundEventPlayerSpawn,
    IRoundEventItemPickup,
    IRoundEventCheckTransmit
{
    private const float ChickenSpeedMultiplier = 1.1f;
    private const int ChickenHealth = 50;
    private const float PlayerScale = 0.2f;
    private const string ChickenModel = "models/chicken/chicken.vmdl";

    private sealed class ChickenState
    {
        public required CCSPlayerPawn Pawn { get; init; }
        public required CBaseModelEntity Chicken { get; init; }
        public required Color Render { get; init; }
        public required float ShadowStrength { get; init; }
        public required float Scale { get; init; }
        public required float VelocityModifier { get; init; }
        public required float MaxSpeed { get; init; }
        public required int Health { get; init; }
        public required int MaxHealth { get; init; }
    }

    private readonly Dictionary<int, ChickenState> _states = new();
    private readonly Dictionary<int, HashSet<uint>> _hiddenEntities = new();
    private bool _active;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "ChickenMode",
        DisplayName = "🐔 我是小鸡",
        Description = "所有玩家都变成了小鸡！移速1.1倍，血量50%！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "player-model-rules",
            "player-scale-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "movement-speed",
            "player-model-control",
            "player-scale-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        context.Effects.RegisterCleanup(() =>
        {
            _active = false;
            RestoreAll();
        });

        foreach (var player in Utilities.GetPlayers())
        {
            ApplyChicken(context, player);
        }

        PrintToChatAll("[娱乐事件] 🐔 我是小鸡：移速提升至1.1倍，最大生命变为50！");
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid;
        var eventContext = context;
        context.Effects.AddTimer(0.1f, () =>
        {
            if (_active)
            {
                ApplyChicken(eventContext, player);
            }
        });
    }

    public void OnItemPickup(in RoundEventContext context, EventItemPickup @event)
    {
        var player = @event.Userid;
        context.Effects.AddTimer(0.05f, () =>
        {
            if (_active && player is { IsValid: true })
            {
                RecordHiddenEntities(player);
            }
        });
    }

    public void OnCheckTransmit(in RoundEventContext context, CCheckTransmitInfoList infoList)
    {
        if (_hiddenEntities.Count == 0)
        {
            return;
        }

        foreach (var (info, observer) in infoList)
        {
            if (observer is not { IsValid: true })
            {
                continue;
            }

            foreach (var (ownerSlot, entities) in _hiddenEntities)
            {
                if (observer.Slot == ownerSlot)
                {
                    continue;
                }

                foreach (var entityIndex in entities)
                {
                    info.TransmitEntities.Remove(entityIndex);
                }
            }
        }
    }

    private void ApplyChicken(in RoundEventContext context, CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        RemoveState(player.Slot);

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        var movementServices = pawn.MovementServices;
        var chicken = CreateChickenModel(context, pawn);
        if (movementServices is null || chicken is null)
        {
            return;
        }

        var state = new ChickenState
        {
            Pawn = pawn,
            Chicken = chicken,
            Render = pawn.Render,
            ShadowStrength = pawn.ShadowStrength,
            Scale = GetScale(pawn),
            VelocityModifier = pawn.VelocityModifier,
            MaxSpeed = movementServices.Maxspeed,
            Health = pawn.Health,
            MaxHealth = pawn.MaxHealth
        };
        _states[player.Slot] = state;

        pawn.Render = Color.FromArgb(0, 255, 255, 255);
        pawn.ShadowStrength = 0.0f;
        SetScale(pawn, PlayerScale);
        pawn.VelocityModifier = ChickenSpeedMultiplier;
        movementServices.Maxspeed = ChickenSpeedMultiplier * 240.0f;
        pawn.Health = ChickenHealth;
        pawn.MaxHealth = ChickenHealth;

        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_flShadowStrength");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
        RecordHiddenEntities(player);
    }

    private static CBaseModelEntity? CreateChickenModel(in RoundEventContext context, CCSPlayerPawn pawn)
    {
        // The chicken model is not authored as a regular dynamic prop. Using the
        // override entity keeps the attached replacement visible to clients.
        var chicken = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (chicken is null)
        {
            return null;
        }

        context.Effects.TrackEntity(chicken);
        var ownerEntity = chicken.CBodyComponent?.SceneNode?.Owner?.Entity;
        if (ownerEntity is not null)
        {
            ownerEntity.Flags &= ~(uint)(1 << 2);
        }

        chicken.SetModel(ChickenModel);
        chicken.Render = Color.FromArgb(255, 255, 255, 255);
        chicken.Teleport(pawn.AbsOrigin, pawn.AbsRotation, null);
        chicken.DispatchSpawn();
        chicken.AcceptInput("InitializeSpawnFromWorld", pawn, pawn);
        chicken.AcceptInput("SetScale", chicken, chicken, "1");
        chicken.AcceptInput("SetParent", pawn, pawn, "!activator");
        Utilities.SetStateChanged(chicken, "CBaseEntity", "m_CBodyComponent");
        return chicken;
    }

    private void RecordHiddenEntities(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        var entities = new HashSet<uint> { pawn.Index };
        if (pawn.WeaponServices is not null)
        {
            foreach (var weapon in pawn.WeaponServices.MyWeapons)
            {
                if (weapon.IsValid)
                {
                    entities.Add(weapon.Index);
                }
            }
        }

        _hiddenEntities[player.Slot] = entities;
    }

    private void RestoreAll()
    {
        foreach (var slot in _states.Keys.ToArray())
        {
            RemoveState(slot);
        }

        _hiddenEntities.Clear();
    }

    private void RemoveState(int slot)
    {
        _hiddenEntities.Remove(slot);
        if (!_states.Remove(slot, out var state))
        {
            return;
        }

        if (state.Chicken.IsValid)
        {
            state.Chicken.Remove();
        }

        var pawn = state.Pawn;
        if (!pawn.IsValid)
        {
            return;
        }

        pawn.Render = state.Render;
        pawn.ShadowStrength = state.ShadowStrength;
        SetScale(pawn, state.Scale);
        pawn.VelocityModifier = state.VelocityModifier;
        if (pawn.MovementServices is not null)
        {
            pawn.MovementServices.Maxspeed = state.MaxSpeed;
        }

        pawn.MaxHealth = state.MaxHealth;
        if (pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
        {
            pawn.Health = Math.Min(state.Health, state.MaxHealth);
        }

        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_flShadowStrength");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
    }

    private static float GetScale(CCSPlayerPawn pawn)
    {
        return pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.Scale ?? 1.0f;
    }

    private static void SetScale(CCSPlayerPawn pawn, float scale)
    {
        var skeleton = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeleton is null)
        {
            return;
        }

        skeleton.Scale = scale;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
        pawn.AcceptInput("SetScale", pawn, pawn, scale.ToString(CultureInfo.InvariantCulture));
    }
}
