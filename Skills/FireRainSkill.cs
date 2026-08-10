using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class FireRainSkill : ISkill, IDecoyStartedSkill
{
    private readonly FireRainService _fireRain;

    public FireRainSkill(FireRainService fireRain)
    {
        _fireRain = fireRain;
    }

    public SkillDescriptor Descriptor { get; } = new()
    {
        Id = "FireRain",
        DisplayName = "火焰雨",
        Description = "投掷诱饵弹，在落点召唤一阵燃烧瓶雨。",
        Kind = SkillKind.Passive,
        Rarity = SkillRarity.Epic,
        DefaultWeight = 10,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "decoy-behavior-control"
        }
    };

    public void OnGranted(in SkillContext context)
    {
        GiveDecoy(context.Player);
    }

    public void OnActivated(in SkillContext context)
    {
    }

    public void OnRevoked(in SkillContext context)
    {
    }

    public void OnDecoyStarted(in SkillContext context, EventDecoyStarted @event)
    {
        var decoy = Utilities.GetEntityFromIndex<CDecoyProjectile>(@event.Entityid);
        if (decoy is { IsValid: true })
        {
            decoy.AddEntityIOEvent("Kill", decoy, delay: 0.1f);
        }

        _fireRain.SpawnRain(context.Player, new Vector(@event.X, @event.Y, @event.Z));
    }

    private static void GiveDecoy(CCSPlayerController player)
    {
        if (!player.IsValid || !player.PawnIsAlive)
        {
            return;
        }

        var alreadyHasDecoy = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.Any(
            weapon => weapon.Value is { IsValid: true, DesignerName: "weapon_decoy" }) == true;
        if (!alreadyHasDecoy)
        {
            player.GiveNamedItem("weapon_decoy");
        }
    }
}
