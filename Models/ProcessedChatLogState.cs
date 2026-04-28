namespace ScumFreeBot.Models;

public sealed class ProcessedChatLogState
{
    public string LastFilePath { get; set; } = string.Empty;
    public long LastBytePosition { get; set; }
}