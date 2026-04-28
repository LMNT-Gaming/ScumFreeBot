using System;
using System.Linq;
using System.Text.RegularExpressions;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class ChatCommandParser
{
    private static readonly Regex LogRegex = new(
        @"^(?<timestamp>\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}):\s'(?<identity>[^']+)'\s'(?<channel>[^:]+):\s(?<message>.*)'$",
        RegexOptions.Compiled);

    public ChatCommand? TryParse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var match = LogRegex.Match(line);
        if (!match.Success)
            return null;

        var identity = match.Groups["identity"].Value.Trim();
        var channel = match.Groups["channel"].Value.Trim();
        var message = match.Groups["message"].Value.Trim();

        if (!message.StartsWith("!"))
            return null;

        var identityParts = identity.Split(':', 2);
        if (identityParts.Length != 2)
            return null;

        var steamId = identityParts[0].Trim();
        var rawPlayerName = identityParts[1].Trim();

        var playerName = Regex.Replace(rawPlayerName, @"\(\d+\)$", "").Trim();

        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        return new ChatCommand
        {
            SteamId = steamId,
            PlayerName = playerName,
            Channel = channel,
            Command = parts[0].ToLowerInvariant(),
            Arguments = parts.Skip(1).ToList(),
            RawLine = line
        };
    }
}