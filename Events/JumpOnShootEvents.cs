using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Events;

public sealed class JumpOnShootEvent : RoundEventBase, IRoundEventWeaponFire
{
    private const float JumpVelocity = 300.0f;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "JumpOnShoot",
        DisplayName = "射击跳跃",
        Description = "开枪时会自动跳跃！仅在地面时触发！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "shoot-jump-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "jump-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        PrintToChatAll("[娱乐事件] 射击跳跃：在地面开枪时会自动跳跃！");
    }

    public void OnWeaponFire(in RoundEventContext context, EventWeaponFire @event)
    {
        var pawn = GetAlivePawn(@event.Userid);
        if (pawn is null || !((PlayerFlags)pawn.Flags).HasFlag(PlayerFlags.FL_ONGROUND))
        {
            return;
        }

        pawn.AbsVelocity.Z = JumpVelocity;
    }

    private static CCSPlayerPawn? GetAlivePawn(CCSPlayerController? player)
    {
        if (player is null || !player.IsValid || !player.PawnIsAlive)
        {
            return null;
        }

        var pawn = player.PlayerPawn.Value;
        return pawn is { IsValid: true } ? pawn : null;
    }
}

public sealed class JumpPlusPlusEvent : RoundEventBase, IRoundEventWeaponFire
{
    private const float JumpVelocity = 400.0f;

    public override EventDescriptor Descriptor { get; } = new()
    {
        Id = "JumpPlusPlus",
        DisplayName = "🦘 超级跳跃",
        Description = "开枪自动跳跃且无扩散！免疫落地伤害！",
        DefaultWeight = 10,
        ExclusiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "shoot-jump-rules",
            "weapon-spread-rules"
        },
        BlockedSkillTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "jump-control",
            "weapon-spread-control",
            "fall-damage-control"
        }
    };

    public override void OnApplied(in RoundEventContext context)
    {
        ConVarOverrides.Set(context.Effects, "weapon_accuracy_nospread", true);
        ConVarOverrides.Set(context.Effects, "sv_falldamage_scale", 0.0f);
        PrintToChatAll("[娱乐事件] 🦘 超级跳跃：开枪即可起飞，本回合无扩散且免疫落地伤害！");
    }

    public void OnWeaponFire(in RoundEventContext context, EventWeaponFire @event)
    {
        var player = @event.Userid;
        if (player is null || !player.IsValid || !player.PawnIsAlive)
        {
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return;
        }

        pawn.AbsVelocity.Z = JumpVelocity;
    }
}
