using System.Text.Json.Serialization;

namespace Myrt1eSkill_Remake.Configuration;

public sealed class WeaponRouletteEventSettings
{
    [JsonPropertyName("IntervalSeconds")]
    public float IntervalSeconds { get; set; } = 20.0f;

    [JsonPropertyName("PrimaryWeapons")]
    public string[] PrimaryWeapons { get; set; } =
    [
        "weapon_ak47", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_aug", "weapon_sg556",
        "weapon_famas", "weapon_galilar", "weapon_mp9", "weapon_mac10", "weapon_mp7",
        "weapon_ump45", "weapon_p90", "weapon_bizon", "weapon_m249", "weapon_negev",
        "weapon_nova", "weapon_xm1014", "weapon_mag7", "weapon_sawedoff", "weapon_ssg08",
        "weapon_awp", "weapon_g3sg1", "weapon_scar20"
    ];
}
