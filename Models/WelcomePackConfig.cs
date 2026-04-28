using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScumFreeBot.Models;

public sealed class WelcomePackConfig
{
    [JsonPropertyName("items")]
    public List<string> Items { get; set; } = new();
}