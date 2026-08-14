using System.Text.Json.Serialization;

namespace Myrt1eSkill_Remake.Configuration;

public sealed class KingModeEventSettings
{
    [JsonPropertyName("KingHealth")]
    public int KingHealth { get; set; } = 500;
}
