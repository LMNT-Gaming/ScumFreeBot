using System.Collections.Generic;

namespace ScumFreeBot.Models;

public sealed class PlayerStateDb
{
    public Dictionary<string, PlayerState> Players { get; set; } = new();
}