using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Configuration;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

/// <summary>
/// Both teams can see player outlines. A neutral chicken is placed on a safe
/// navigation position; the team that reaches it receives AK-47s.
/// </summary>
public sealed class FindHimEvent : RoundEventBase, IRoundEventTick, IRoundEventPlayerSpawn, IRoundEventItemPickup
{
    private const string GrantId = "event:FindHim";
    private const string ChickenModel = "models/chicken/chicken.vmdl";
    private readonly FindHimEventSettings _settings;
    private readonly NavMeshService _navMesh;
    private readonly WallhackService _wallhack;
    private CChicken? _chicken;
    private CDynamicProp? _glowRelay;
    private CDynamicProp? _glow;
    private bool _found;
    private bool _revealed;

    public FindHimEvent(FindHimEventSettings settings, NavMeshService navMesh, WallhackService wallhack)
    {
        _settings = settings;
        _navMesh = navMesh;
        _wallhack = wallhack;
    }

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "FindHim",
        DisplayName = "找到他",
        Description = "没收所有人的枪械、刀和 C4；找到地图上的目标小鸡后，找到者一方获得 AK-47。",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-loadout-rules", "xray-vision-rules"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        _found = false;
        _revealed = false;
        var effects = context.Effects;
        _wallhack.SetGlobalGrant(GrantId, includeTeammates: true);
        effects.RegisterCleanup(() => _wallhack.RemoveGrant(GrantId));

        foreach (var player in Utilities.GetPlayers())
        {
            RemoveForbiddenWeapons(player);
        }

        SpawnChicken(effects);
        var revealDelay = PositiveOr(_settings.RevealDelaySeconds, 30.0f);
        context.Effects.AddTimer(revealDelay, () =>
        {
            if (!_found && !_revealed && _chicken is { IsValid: true })
            {
                _revealed = true;
                CreateChickenGlow(effects);
                PrintToChatAll("[娱乐事件] 找到他：目标小鸡已获得透视标记！");
            }
        });

        PrintToChatAll("[娱乐事件] 找到他：找到目标小鸡，你和你的队友将获得 AK-47！");
    }

    public void OnTick(in RoundEventContext context)
    {
        if (_found || _chicken is not { IsValid: true })
        {
            return;
        }

        var radius = Math.Max(1.0f, _settings.FindRadius);
        var radiusSquared = radius * radius;
        foreach (var player in Utilities.GetPlayers())
        {
            var pawn = player.PlayerPawn.Value;
            if (player is not { IsValid: true, PawnIsAlive: true }
                || pawn is not { IsValid: true }
                || pawn.AbsOrigin is not { } origin
                || _chicken.AbsOrigin is not { } chickenOrigin)
            {
                continue;
            }

            var dx = origin.X - chickenOrigin.X;
            var dy = origin.Y - chickenOrigin.Y;
            var dz = origin.Z - chickenOrigin.Z;
            if (dx * dx + dy * dy + dz * dz <= radiusSquared)
            {
                Resolve(player, context);
                return;
            }
        }
    }

    public void OnPlayerSpawn(in RoundEventContext context, EventPlayerSpawn @event)
    {
        if (_found)
        {
            return;
        }

        context.Effects.AddTimer(0.2f, () =>
        {
            if (!_found)
            {
                RemoveForbiddenWeapons(@event.Userid);
            }
        });
    }

    public void OnItemPickup(in RoundEventContext context, EventItemPickup @event)
    {
        if (_found)
        {
            return;
        }

        context.Effects.AddTimer(0.02f, () =>
        {
            if (!_found)
            {
                RemoveForbiddenWeapons(@event.Userid);
            }
        });
    }

    private void SpawnChicken(EffectScope effects)
    {
        var probe = Utilities.GetPlayers().FirstOrDefault(player => player is { IsValid: true, PawnIsAlive: true });
        if (probe is null || !_navMesh.TryFindSafeRandomPosition(probe, out var position, out _))
        {
            PrintToChatAll("[找到他] 当前地图没有可用的安全导航位置，目标未生成。");
            return;
        }

        var chicken = Utilities.CreateEntityByName<CChicken>("chicken");
        if (chicken is null)
        {
            return;
        }

        chicken.DispatchSpawn();
        var health = Math.Clamp(_settings.ChickenHealth, 1, 10000);
        chicken.MaxHealth = health;
        chicken.Health = health;
        chicken.Render = Color.FromArgb(255, 255, 220, 40);
        chicken.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        Utilities.SetStateChanged(chicken, "CBaseModelEntity", "m_clrRender");
        Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iMaxHealth");
        _chicken = chicken;
        effects.TrackEntity(chicken);
    }

    private void Resolve(CCSPlayerController finder, in RoundEventContext context)
    {
        _found = true;
        var team = finder.Team;
        foreach (var player in Utilities.GetPlayers())
        {
            if (player is { IsValid: true, PawnIsAlive: true } && player.Team == team)
            {
                RemoveForbiddenWeapons(player);
                player.GiveNamedItem("weapon_ak47");
            }
        }

        RemoveGlow();
        if (_chicken is { IsValid: true })
        {
            _chicken.Remove();
        }

        PrintToChatAll($"[娱乐事件] 找到他：{finder.PlayerName} 找到了目标！{(team == CsTeam.Terrorist ? "T" : "CT")} 队获得 AK-47。");
    }

    private void CreateChickenGlow(EffectScope effects)
    {
        if (_chicken is not { IsValid: true } || _glow is { IsValid: true })
        {
            return;
        }

        var relay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        var glow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (relay is null || glow is null)
        {
            relay?.Remove();
            glow?.Remove();
            return;
        }

        relay.SetModel(ChickenModel);
        relay.RenderMode = RenderMode_t.kRenderNone;
        relay.DispatchSpawn();
        glow.SetModel(ChickenModel);
        glow.Render = Color.FromArgb(1, 255, 40, 40);
        glow.Glow.GlowColorOverride = Color.FromArgb(255, 255, 40, 40);
        glow.Glow.GlowRange = 5000;
        glow.Glow.GlowRangeMin = 0;
        glow.Glow.GlowType = 3;
        glow.Glow.GlowTeam = -1;
        glow.DispatchSpawn();
        relay.AcceptInput("FollowEntity", _chicken, relay, "!activator");
        glow.AcceptInput("FollowEntity", relay, glow, "!activator");
        _glowRelay = relay;
        _glow = glow;
        effects.TrackEntity(relay);
        effects.TrackEntity(glow);
    }

    private void RemoveGlow()
    {
        if (_glow is { IsValid: true }) _glow.Remove();
        if (_glowRelay is { IsValid: true }) _glowRelay.Remove();
        _glow = null;
        _glowRelay = null;
    }

    private static void RemoveForbiddenWeapons(CCSPlayerController? player)
    {
        var weapons = player?.PlayerPawn.Value?.WeaponServices;
        if (player is not { IsValid: true } || weapons is null)
        {
            return;
        }

        foreach (var handle in weapons.MyWeapons.ToArray())
        {
            var weapon = handle.Value;
            if (weapon is not { IsValid: true }) continue;
            var name = weapon.DesignerName;
            var type = weapon.As<CCSWeaponBase>().VData?.WeaponType;
            if (name.Equals("weapon_c4", StringComparison.OrdinalIgnoreCase)
                || name.Contains("knife", StringComparison.OrdinalIgnoreCase)
                || type == CSWeaponType.WEAPONTYPE_KNIFE
                || BirdshotKingEvent.IsPrimaryOrSecondary(weapon))
            {
                weapon.Remove();
            }
        }
    }

    private static float PositiveOr(float value, float fallback) => float.IsFinite(value) && value > 0 ? value : fallback;
}
