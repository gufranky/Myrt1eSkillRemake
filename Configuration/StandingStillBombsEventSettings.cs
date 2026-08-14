using System.Text.Json.Serialization;

namespace Myrt1eSkill_Remake.Configuration;

public sealed class StandingStillBombsEventSettings
{
    [JsonPropertyName("IntervalSeconds")]
    public float IntervalSeconds { get; set; } = 5.0f;

    [JsonPropertyName("FuseSeconds")]
    public float FuseSeconds { get; set; } = 2.0f;

    [JsonPropertyName("Damage")]
    public float Damage { get; set; } = 100.0f;

    [JsonPropertyName("Radius")]
    public float Radius { get; set; } = 250.0f;
}
