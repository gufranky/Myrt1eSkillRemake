using System.Text.Json.Serialization;

namespace Myrt1eSkill_Remake.Configuration;

public sealed class HelpingHandSettings
{
    [JsonPropertyName("DurationSeconds")] public float DurationSeconds { get; set; } = 5.0f;
    [JsonPropertyName("SpeedMultiplier")] public float SpeedMultiplier { get; set; } = 1.5f;
    [JsonPropertyName("JumpHeightMultiplier")] public float JumpHeightMultiplier { get; set; } = 1.35f;
}
