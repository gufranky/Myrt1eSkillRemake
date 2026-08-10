using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class DecoyTeleportEvent : RoundEventBase, IRoundEventDecoyStarted, IRoundEventPlayerSpawn
{
    private bool _active;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "DecoyTeleport",
        DisplayName = "🎯 TP弹模式",
        Description = "投掷诱饵弹后会传送到落点！每回合自动获得诱饵弹！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "decoy-behavior-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "decoy-behavior-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        context.Effects.RegisterCleanup(() => _active = false);

        foreach (var player in Utilities.GetPlayers())
        {
            GiveDecoy(player);
        }

        PrintToChatAll("[娱乐事件] 🎯 TP弹模式：诱饵弹落地后会将投掷者传送过去！");
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid;
        context.Effects.AddTimer(0.1f, () =>
        {
            if (_active)
            {
                GiveDecoy(player);
            }
        });
    }

    public void OnDecoyStarted(in RoundEventContext context, EventDecoyStarted @event)
    {
        var player = @event.Userid;
        if (!IsAlive(player))
        {
            return;
        }

        var pawn = player!.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        var originalCollisionGroup = pawn.Collision.CollisionGroup;
        var originalAttributeGroup = pawn.Collision.CollisionAttribute.CollisionGroup;
        pawn.Teleport(new Vector(@event.X, @event.Y, @event.Z), pawn.AbsRotation, new Vector(0.0f, 0.0f, 0.0f));

        pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
        pawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
        MarkCollisionChanged(pawn);

        Server.NextFrame(() =>
        {
            if (pawn is not { IsValid: true })
            {
                return;
            }

            pawn.Collision.CollisionGroup = originalCollisionGroup;
            pawn.Collision.CollisionAttribute.CollisionGroup = originalAttributeGroup;
            MarkCollisionChanged(pawn);

            if (_active)
            {
                GiveDecoy(player);
            }
        });

        PluginText.Center(player, "🎯 已传送到诱饵弹落点！");
    }

    private static void GiveDecoy(CCSPlayerController? player)
    {
        if (!IsAlive(player))
        {
            return;
        }

        var pawn = player!.PlayerPawn.Value;
        var alreadyHasDecoy = pawn?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_decoy" }) == true;
        if (!alreadyHasDecoy)
        {
            player.GiveNamedItem("weapon_decoy");
        }
    }

    private static bool IsAlive(CCSPlayerController? player)
    {
        return player is { IsValid: true, PawnIsAlive: true };
    }

    private static void MarkCollisionChanged(CCSPlayerPawn pawn)
    {
        Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_CollisionGroup");
        Utilities.SetStateChanged(pawn, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
    }
}
