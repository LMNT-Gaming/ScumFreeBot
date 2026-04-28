using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class PlayerLocationService
{
    private static readonly Regex SteamLineRegex = new(
        @"^Steam:\s*(?<name>.+?)\s*\((?<steamId>\d+)\)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LocationLineRegex = new(
        @"^Location:\s*(?<location>X\s*=\s*-?\d+(?:\.\d+)?\s+Y\s*=\s*-?\d+(?:\.\d+)?\s+Z\s*=\s*-?\d+(?:\.\d+)?)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string ScumLogPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SCUM",
        "Saved",
        "Logs",
        "SCUM.log");

    public bool ScriptNeedsPlayerLocation(string scriptText)
    {
        return scriptText.Contains("{playerlocation}", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string?> ResolvePlayerLocationAsync(
        string autoHotkeyPath,
        string ahkScriptPath,
        ChatCommand chatCommand,
        CommandSenderService commandSenderService)
    {
        await commandSenderService.SendCommandAsync(autoHotkeyPath, ahkScriptPath, "#ListPlayers");

        // SCUM braucht kurz, bis #ListPlayers in SCUM.log geschrieben wurde.
        await Task.Delay(1500);

        return await ReadPlayerLocationFromLogAsync(chatCommand);
    }

    public async Task<string?> ReadPlayerLocationFromLogAsync(ChatCommand chatCommand)
    {
        if (!File.Exists(ScumLogPath))
        {
            return null;
        }

        string[] lines;

        // SCUM.log ist offen/gelockt. Deshalb FileShare.ReadWrite.
        await using (var stream = new FileStream(
                         ScumLogPath,
                         FileMode.Open,
                         FileAccess.Read,
                         FileShare.ReadWrite | FileShare.Delete))
        using (var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), detectEncodingFromByteOrderMarks: true))
        {
            var text = await reader.ReadToEndAsync();
            lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        // Von unten nach oben suchen, damit wir immer das neueste #ListPlayers-Ergebnis nehmen.
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var steamMatch = SteamLineRegex.Match(lines[i].Trim());
            if (!steamMatch.Success)
            {
                continue;
            }

            var name = steamMatch.Groups["name"].Value.Trim();
            var steamId = steamMatch.Groups["steamId"].Value.Trim();

            var isTargetPlayer =
                (!string.IsNullOrWhiteSpace(chatCommand.SteamId) &&
                 string.Equals(steamId, chatCommand.SteamId.Trim(), StringComparison.OrdinalIgnoreCase))
                ||
                string.Equals(name, chatCommand.PlayerName.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!isTargetPlayer)
            {
                continue;
            }

            // Location steht ein paar Zeilen nach Steam.
            for (var j = i + 1; j < Math.Min(i + 10, lines.Length); j++)
            {
                var locationMatch = LocationLineRegex.Match(lines[j].Trim());
                if (locationMatch.Success)
                {
                    return NormalizeLocation(locationMatch.Groups["location"].Value);
                }
            }
        }

        return null;
    }

    private static string NormalizeLocation(string location)
    {
        return Regex.Replace(location.Trim(), @"\s+", " ");
    }
}