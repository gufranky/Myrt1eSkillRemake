using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Myrt1eSkill_Remake.Core;

namespace Myrt1eSkill_Remake.Skills;

public sealed class WeaponSwapSkill : ISkill, IConditionalActivationSkill
{
    public const float ActivationCooldownSeconds = 30.0f;

    private sealed record WeaponSnapshot(string Name, int Clip1, int Clip2, int Reserve);
    private sealed record GearSnapshot(int Health, int Armor, bool HasHelmet, bool HasDefuser);

    public static SkillDescriptor Definition { get; } = new()
    {
        Id = "WeaponSwap",
        DisplayName = "🔁 武器交换",
        Description = "按 E 与一名随机存活敌人交换整套武器，冷却 30 秒。",
        Kind = SkillKind.Active,
        Rarity = SkillRarity.Common,
        DefaultWeight = 10,
        CooldownSeconds = ActivationCooldownSeconds,
        ConflictTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "weapon-inventory-replacement"
        }
    };

    public SkillDescriptor Descriptor => Definition;

    public void OnGranted(in SkillContext context)
    {
    }

    public bool TryActivate(in SkillContext context)
    {
        var player = context.Player;
        if (!IsAlive(player))
        {
            return false;
        }

        var playerWeapons = SnapshotWeapons(player, out var playerHasC4);
        if (playerWeapons.Count == 0)
        {
            PluginText.Chat(player, "[武器交换] 你当前没有可以交换的武器。");
            return false;
        }

        var candidates = Utilities.GetPlayers()
            .Where(enemy => IsAlive(enemy)
                            && enemy.Slot != player.Slot
                            && enemy.Team != player.Team)
            .Select(enemy =>
            {
                var weapons = SnapshotWeapons(enemy, out var hasC4);
                return (Enemy: enemy, Weapons: weapons, HasC4: hasC4);
            })
            .Where(entry => entry.Weapons.Count > 0)
            .ToArray();
        if (candidates.Length == 0)
        {
            PluginText.Chat(player, "[武器交换] 当前没有持有武器的存活敌人。");
            return false;
        }

        var selected = candidates[Random.Shared.Next(candidates.Length)];
        var enemy = selected.Enemy;
        var playerIndex = player.Index;
        var enemyIndex = enemy.Index;

        Server.NextFrame(() =>
        {
            var currentPlayer = Utilities.GetPlayerFromIndex((int)playerIndex);
            var currentEnemy = Utilities.GetPlayerFromIndex((int)enemyIndex);
            if (!IsAlive(currentPlayer) || !IsAlive(currentEnemy))
            {
                return;
            }

            SwapInventories(
                currentPlayer!,
                currentEnemy!,
                playerWeapons,
                selected.Weapons,
                playerHasC4,
                selected.HasC4);
            PluginText.Chat(currentPlayer!, $"[武器交换] 已与 {currentEnemy!.PlayerName} 交换武器。");
            PluginText.Chat(currentEnemy!, $"[武器交换] {currentPlayer!.PlayerName} 与你交换了武器。");
        });

        return true;
    }

    public void OnActivated(in SkillContext context) => _ = TryActivate(context);

    public void OnRevoked(in SkillContext context)
    {
    }

    public static bool IsC4(string designerName) =>
        designerName.Equals("weapon_c4", StringComparison.OrdinalIgnoreCase);

    private static void SwapInventories(
        CCSPlayerController player,
        CCSPlayerController enemy,
        IReadOnlyList<WeaponSnapshot> playerWeapons,
        IReadOnlyList<WeaponSnapshot> enemyWeapons,
        bool playerHasC4,
        bool enemyHasC4)
    {
        var playerGear = SnapshotGear(player);
        var enemyGear = SnapshotGear(enemy);

        player.RemoveWeapons();
        enemy.RemoveWeapons();
        GiveWeapons(player, enemyWeapons, playerHasC4);
        GiveWeapons(enemy, playerWeapons, enemyHasC4);
        RestoreGear(player, playerGear);
        RestoreGear(enemy, enemyGear);
    }

    private static IReadOnlyList<WeaponSnapshot> SnapshotWeapons(
        CCSPlayerController player,
        out bool hasC4)
    {
        hasC4 = false;
        var weapons = new List<WeaponSnapshot>();
        var handles = player.PlayerPawn.Value?.WeaponServices?.MyWeapons;
        if (handles is null)
        {
            return weapons;
        }

        foreach (var handle in handles)
        {
            var weapon = handle.Value;
            if (weapon is not { IsValid: true } || string.IsNullOrWhiteSpace(weapon.DesignerName))
            {
                continue;
            }

            if (IsC4(weapon.DesignerName))
            {
                hasC4 = true;
                continue;
            }

            weapons.Add(new WeaponSnapshot(
                weapon.DesignerName,
                weapon.Clip1,
                weapon.Clip2,
                weapon.ReserveAmmo.Length > 0 ? weapon.ReserveAmmo[0] : 0));
        }

        return weapons;
    }

    private static GearSnapshot? SnapshotGear(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn is not { IsValid: true })
        {
            return null;
        }

        var items = pawn.ItemServices?.As<CCSPlayer_ItemServices>();
        return new GearSnapshot(
            pawn.Health,
            pawn.ArmorValue,
            items?.HasHelmet ?? false,
            items?.HasDefuser ?? false);
    }

    private static void GiveWeapons(
        CCSPlayerController player,
        IReadOnlyList<WeaponSnapshot> weapons,
        bool giveC4)
    {
        foreach (var weapon in weapons)
        {
            player.GiveNamedItem(weapon.Name);
        }

        if (giveC4)
        {
            player.GiveNamedItem("weapon_c4");
        }

        var index = player.Index;
        Server.NextFrame(() => RestoreAmmo(index, weapons));
    }

    private static void RestoreAmmo(uint playerIndex, IReadOnlyList<WeaponSnapshot> snapshots)
    {
        var player = Utilities.GetPlayerFromIndex((int)playerIndex);
        var weapons = player?.PlayerPawn.Value?.WeaponServices?.MyWeapons;
        if (!IsAlive(player) || weapons is null)
        {
            return;
        }

        foreach (var handle in weapons)
        {
            var weapon = handle.Value;
            var snapshot = weapon is { IsValid: true }
                ? snapshots.FirstOrDefault(item => item.Name.Equals(
                    weapon.DesignerName,
                    StringComparison.OrdinalIgnoreCase))
                : null;
            if (weapon is not { IsValid: true } || snapshot is null)
            {
                continue;
            }

            weapon.Clip1 = snapshot.Clip1;
            weapon.Clip2 = snapshot.Clip2;
            if (weapon.ReserveAmmo.Length > 0)
            {
                weapon.ReserveAmmo.Fill(snapshot.Reserve);
            }

            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip2");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
        }
    }

    private static void RestoreGear(CCSPlayerController player, GearSnapshot? gear)
    {
        var pawn = player.PlayerPawn.Value;
        if (gear is null || pawn is not { IsValid: true })
        {
            return;
        }

        pawn.Health = gear.Health;
        pawn.ArmorValue = gear.Armor;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        var items = pawn.ItemServices?.As<CCSPlayer_ItemServices>();
        if (items is not null)
        {
            items.HasHelmet = gear.HasHelmet;
            items.HasDefuser = gear.HasDefuser;
        }
    }

    private static bool IsAlive(CCSPlayerController? player) =>
        player is { IsValid: true, PawnIsAlive: true }
        && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist;
}
