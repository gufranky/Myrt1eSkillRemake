using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;

namespace Myrt1eSkill_Remake.Core;

/// <summary>
/// Temporarily replaces the engine's nested bullet hit-group value. The
/// original value is restored by DamageEventRouter's post-damage callback.
/// Entries are paired with their CTakeDamageInfo handle so nested damage cannot
/// accidentally restore the outer hit early.
/// </summary>
public static class AimbotHitGroupService
{
    private const int HitGroupPointerOffset = 16;
    private const int HitGroupValueOffset = 56;

    private sealed record RestoreEntry(
        nint DamageInfoHandle,
        nint Address,
        HitGroup_t OriginalHitGroup);

    private static readonly ThreadLocal<Stack<RestoreEntry>> RestoreStack =
        new(() => new Stack<RestoreEntry>());

    private static readonly HashSet<string> BulletWeapons = new(StringComparer.Ordinal)
    {
        "deagle", "revolver", "glock", "usp_silencer", "cz75a",
        "fiveseven", "p250", "tec9", "elite", "hkp2000",
        "mp9", "mac10", "bizon", "mp7", "ump45", "p90", "mp5sd",
        "famas", "galilar", "m4a1", "m4a1_silencer", "ak47", "aug", "sg556",
        "ssg08", "awp", "scar20", "g3sg1",
        "nova", "xm1014", "mag7", "sawedoff",
        "m249", "negev"
    };

    public static bool TryForceHeadshot(CTakeDamageInfo? damageInfo)
    {
        if (damageInfo is null
            || damageInfo.Handle == nint.Zero
            || !IsBulletDamage(damageInfo)
            || !TryGetHitGroupAddress(damageInfo, out var address))
        {
            return false;
        }

        var original = (HitGroup_t)Marshal.ReadInt32(address);
        if (original is HitGroup_t.HITGROUP_HEAD or HitGroup_t.HITGROUP_INVALID)
        {
            return false;
        }

        RestoreStack.Value!.Push(new RestoreEntry(damageInfo.Handle, address, original));
        Marshal.WriteInt32(address, (int)HitGroup_t.HITGROUP_HEAD);
        return true;
    }

    public static void Restore(CTakeDamageInfo? damageInfo)
    {
        if (damageInfo is null || damageInfo.Handle == nint.Zero)
        {
            return;
        }

        var stack = RestoreStack.Value!;
        if (stack.Count == 0 || stack.Peek().DamageInfoHandle != damageInfo.Handle)
        {
            return;
        }

        var entry = stack.Pop();
        if (entry.Address != nint.Zero)
        {
            Marshal.WriteInt32(entry.Address, (int)entry.OriginalHitGroup);
        }
    }

    public static bool FiresBullets(string? designerName)
    {
        if (string.IsNullOrWhiteSpace(designerName))
        {
            return false;
        }

        const string prefix = "weapon_";
        var normalized = designerName.StartsWith(prefix, StringComparison.Ordinal)
            ? designerName[prefix.Length..]
            : designerName;
        return BulletWeapons.Contains(normalized);
    }

    private static bool IsBulletDamage(CTakeDamageInfo damageInfo)
    {
        var ability = damageInfo.Ability?.Value;
        return ability is { IsValid: true } && FiresBullets(ability.DesignerName);
    }

    private static bool TryGetHitGroupAddress(CTakeDamageInfo damageInfo, out nint address)
    {
        address = nint.Zero;
        int offset;
        try
        {
            offset = GameData.GetOffset("CTakeDamageInfo_HitGroup");
        }
        catch
        {
            return false;
        }

        if (offset <= 0)
        {
            return false;
        }

        var hitGroupPointer = Marshal.ReadIntPtr(damageInfo.Handle, offset);
        if (hitGroupPointer == nint.Zero)
        {
            return false;
        }

        var hitGroupData = Marshal.ReadIntPtr(hitGroupPointer, HitGroupPointerOffset);
        if (hitGroupData == nint.Zero)
        {
            return false;
        }

        address = hitGroupData + HitGroupValueOffset;
        return true;
    }
}
