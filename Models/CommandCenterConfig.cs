using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScumFreeBot.Models;

public sealed class CommandCenterConfig
{
    [JsonPropertyName("commands")]
    public List<CommandRule> Commands { get; set; } = new();
}
