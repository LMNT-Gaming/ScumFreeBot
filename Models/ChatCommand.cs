namespace ScumFreeBot.Models;

public sealed class ChatCommand
{
    public string SteamId { get; init; } = string.Empty;
    public string PlayerName { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public List<string> Arguments { get; init; } = new();
    public string RawLine { get; init; } = string.Empty;
}