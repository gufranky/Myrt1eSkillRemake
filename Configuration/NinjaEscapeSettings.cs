using System.Text.Json.Serialization;

namespace Myrt1eSkill_Remake.Configuration;

public sealed class NinjaEscapeSettings
{
    [JsonPropertyName("MaximumUsesPerRound")]
    public int MaximumUsesPerRound { get; set; } = 1;
}
