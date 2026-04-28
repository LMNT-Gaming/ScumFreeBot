using System.Text.Json.Serialization;

namespace ScumFreeBot.Models;

public sealed class CommandRule
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = string.Empty;

    [JsonPropertyName("scriptFile")]
    public string ScriptFile { get; set; } = string.Empty;

    [JsonPropertyName("runMode")]
    public string RunMode { get; set; } = "Always";

    [JsonPropertyName("cooldownHours")]
    public double CooldownHours { get; set; }

    [JsonPropertyName("zone")]
    public string Zone { get; set; } = string.Empty;

    [JsonPropertyName("denyMessage")]
    public string DenyMessage { get; set; } = string.Empty;
}
