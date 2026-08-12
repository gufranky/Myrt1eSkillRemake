using System.Text.Json.Serialization;

namespace Myrt1eSkill_Remake.Configuration;

public sealed class SpecialHeartSettings
{
    [JsonPropertyName("ChickenHealth")]
    public int ChickenHealth { get; set; } = 50;

    [JsonPropertyName("SpeedMultiplier")]
    public float SpeedMultiplier { get; set; } = 3.0f;

    [JsonPropertyName("MaximumExtraStep")]
    public float MaximumExtraStep { get; set; } = 18.0f;
}
