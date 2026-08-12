using System.Text.Json.Serialization;
namespace Myrt1eSkill_Remake.Configuration;
public sealed class BombardmentZoneSettings
{
    [JsonPropertyName("Damage")] public float Damage { get; set; } = 20.0f;
    [JsonPropertyName("Radius")] public float Radius { get; set; } = 180.0f;
}
