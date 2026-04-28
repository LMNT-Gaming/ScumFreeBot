using System;
using System.Collections.Generic;

namespace ScumFreeBot.Models;

public sealed class PlayerState
{
    public string PlayerName { get; set; } = string.Empty;
    public bool WelcomePackReceived { get; set; }
    public DateTime? WelcomePackReceivedAtUtc { get; set; }
    public Dictionary<string, PlayerCommandState> CommandStates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
