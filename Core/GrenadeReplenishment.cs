namespace Myrt1eSkill_Remake.Core;

public static class GrenadeReplenishment
{
    // EventGrenadeThrown can arrive before the thrown weapon is removed from
    // the player's inventory. Allow several server ticks before checking it.
    public const float DelaySeconds = 0.3f;

    public static bool Matches(string? eventWeapon, string bareWeaponName) =>
        string.Equals(eventWeapon, bareWeaponName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(eventWeapon, $"weapon_{bareWeaponName}", StringComparison.OrdinalIgnoreCase);

    public static string ToDesignerName(string eventWeapon) =>
        eventWeapon.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase)
            ? eventWeapon
            : $"weapon_{eventWeapon}";
}
