using System;

namespace ScumFreeBot.Models;

public sealed class PlayerCommandState
{
    public int UseCount { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
}
