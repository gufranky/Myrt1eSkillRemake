using System.Text.Json.Serialization;
namespace Myrt1eSkill_Remake.Configuration;
public sealed class GiantEventSettings
{
    [JsonPropertyName("PlayerScale")] public float PlayerScale { get; set; } = 1.5f;
    [JsonPropertyName("Health")] public int Health { get; set; } = 300;
}
