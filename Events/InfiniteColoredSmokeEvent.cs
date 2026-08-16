using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class InfiniteColoredSmokeEvent : RoundEventBase,
    IRoundEventGrenadeThrown,
    IRoundEventPlayerSpawn,
    IRoundEventEntitySpawned
{
    private bool _active;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "InfiniteColoredSmoke",
        DisplayName = "🌈 无限彩烟",
        Description = "所有玩家拥有无限烟雾弹，每颗烟雾都会随机生成一种 RGB 颜色。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "smoke-grenade-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "smoke-behavior-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _active = true;
        context.Effects.RegisterCleanup(() => _active = false);

        foreach (var player in Utilities.GetPlayers())
        {
            ScheduleInitialSmoke(context, player);
        }

        PrintToChatAll("[娱乐事件] 🌈 无限彩烟：投出烟雾弹后会自动补充，每颗烟雾颜色随机！");
    }

    public void OnGrenadeThrown(in RoundEventContext context, EventGrenadeThrown @event)
    {
        if (!_active || !GrenadeReplenishment.Matches(@event.Weapon, "smokegrenade"))
        {
            return;
        }

        var player = @event.Userid;
        context.Effects.AddTimer(GrenadeReplenishment.DelaySeconds, () =>
        {
            if (_active)
            {
                GiveReplacementSmoke(player);
            }
        });
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid;
        ScheduleInitialSmoke(context, player);
    }

    public void OnEntitySpawned(in RoundEventContext context, CEntityInstance entity)
    {
        if (!_active || !string.Equals(entity.DesignerName, "smokegrenade_projectile", StringComparison.Ordinal))
        {
            return;
        }

        var smoke = entity.As<CSmokeGrenadeProjectile>();
        if (smoke is not { IsValid: true })
        {
            return;
        }

        var red = Random.Shared.Next(0, 256);
        var green = Random.Shared.Next(0, 256);
        var blue = Random.Shared.Next(0, 256);
        Server.NextFrame(() =>
        {
            if (!_active || !smoke.IsValid)
            {
                return;
            }

            smoke.SmokeColor.X = red;
            smoke.SmokeColor.Y = green;
            smoke.SmokeColor.Z = blue;
            Utilities.SetStateChanged(smoke, "CSmokeGrenadeProjectile", "m_vSmokeColor");
        });
    }

    private void ScheduleInitialSmoke(in RoundEventContext context, CCSPlayerController? player)
    {
        // RoundStart and PlayerSpawn can both arrive before the pawn is marked
        // alive. Retry during the spawn transition, but retain the inventory
        // check here so a normal smoke grenade is never duplicated.
        GiveSmokeIfMissing(player);
        context.Effects.AddTimer(0.20f, () =>
        {
            if (_active)
            {
                GiveSmokeIfMissing(player);
            }
        });
        context.Effects.AddTimer(0.75f, () =>
        {
            if (_active)
            {
                GiveSmokeIfMissing(player);
            }
        });
    }

    private static void GiveSmokeIfMissing(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, PawnIsAlive: true })
        {
            return;
        }

        var alreadyHasSmoke = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_smokegrenade" }) == true;
        if (!alreadyHasSmoke)
        {
            player.GiveNamedItem("weapon_smokegrenade");
        }
    }

    private static void GiveReplacementSmoke(CCSPlayerController? player)
    {
        if (player is { IsValid: true, PawnIsAlive: true })
        {
            // The thrown-weapon handle may survive in MyWeapons for longer
            // than the grenade-thrown callback. Do not use MyWeapons to
            // decide whether a replacement is needed on this path.
            player.GiveNamedItem("weapon_smokegrenade");
        }
    }
}
