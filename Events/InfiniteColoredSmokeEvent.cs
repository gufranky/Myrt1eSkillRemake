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
            GiveSmoke(player);
        }

        PrintToChatAll("[娱乐事件] 🌈 无限彩烟：投出烟雾弹后会自动补充，每颗烟雾颜色随机！");
    }

    public void OnGrenadeThrown(in RoundEventContext context, EventGrenadeThrown @event)
    {
        if (!_active || !string.Equals(@event.Weapon, "smokegrenade", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var player = @event.Userid;
        context.Effects.AddTimer(0.01f, () =>
        {
            if (_active)
            {
                GiveSmoke(player);
            }
        });
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        var player = @event.Userid;
        context.Effects.AddTimer(0.1f, () =>
        {
            if (_active)
            {
                GiveSmoke(player);
            }
        });
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
        context.Effects.AddTimer(0.01f, () =>
        {
            if (!_active || !smoke.IsValid)
            {
                return;
            }

            smoke.SmokeColor.X = red;
            smoke.SmokeColor.Y = green;
            smoke.SmokeColor.Z = blue;
        });
    }

    private static void GiveSmoke(CCSPlayerController? player)
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
}
