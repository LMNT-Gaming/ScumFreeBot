using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class CommandScriptRunnerService
{
    private static readonly Random Random = new();

    private static readonly Regex WaitRegex = new(
        @"^wait\s+(?<value>\d+(?:[\.,]\d+)?)(?<unit>ms|s|m|h)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PlayerLocationPlaceholderRegex = new(
        @"\{playerlocation(?<offset>[+-]\d+(?:\.\d+)?)?\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RandomCaseRegex = new(
        @"^case(?:\s+(?<weight>\d+(?:[\.,]\d+)?))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly CommandSenderService _commandSenderService;
    private readonly PlayerLocationService _playerLocationService;

    public CommandScriptRunnerService(
        CommandSenderService commandSenderService,
        PlayerLocationService playerLocationService)
    {
        _commandSenderService = commandSenderService;
        _playerLocationService = playerLocationService;
    }

    private sealed class RandomCaseBlock
    {
        public double Weight { get; init; } = 1;
        public List<string> Lines { get; } = new();
    }

    public async Task RunAsync(string autoHotkeyPath, string ahkScriptPath, CommandRule rule, ChatCommand chatCommand)
    {
        var scriptPath = Path.Combine(AppPaths.DataDirectory, rule.ScriptFile);
        if (!File.Exists(scriptPath))
        {
            await _commandSenderService.SendCommandAsync(
                autoHotkeyPath,
                ahkScriptPath,
                $"Script nicht gefunden: {rule.ScriptFile}");

            return;
        }

        var lines = await File.ReadAllLinesAsync(scriptPath);

        // Randomblöcke zuerst auflösen.
        // Dadurch wird {playerlocation} nur abgefragt, wenn der gewählte case es wirklich nutzt.
        lines = ExpandRandomBlocks(lines);

        var scriptText = string.Join(Environment.NewLine, lines);

        string? playerLocation = null;

        if (_playerLocationService.ScriptNeedsPlayerLocation(scriptText))
        {
            playerLocation = await _playerLocationService.ResolvePlayerLocationAsync(
                autoHotkeyPath,
                ahkScriptPath,
                chatCommand,
                _commandSenderService);

            if (string.IsNullOrWhiteSpace(playerLocation))
            {
                await _commandSenderService.SendCommandAsync(
                    autoHotkeyPath,
                    ahkScriptPath,
                    $"Position von {chatCommand.PlayerName} konnte nicht ermittelt werden.");

                return;
            }
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line) ||
                line.StartsWith("# ") ||
                line.StartsWith("//") ||
                line.StartsWith(";"))
            {
                continue;
            }

            if (TryParseWait(line, out var delay))
            {
                await Task.Delay(delay);
                continue;
            }

            var commandText = ReplacePlaceholders(line, chatCommand, playerLocation);

            await _commandSenderService.SendCommandAsync(
                autoHotkeyPath,
                ahkScriptPath,
                commandText);
        }
    }

    private static string[] ExpandRandomBlocks(string[] lines)
    {
        var result = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (!line.Equals("randomblock", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(lines[i]);
                continue;
            }

            var cases = new List<RandomCaseBlock>();
            RandomCaseBlock? currentCase = null;

            i++;

            for (; i < lines.Length; i++)
            {
                var innerLine = lines[i].Trim();

                if (innerLine.Equals("endrandomblock", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                var caseMatch = RandomCaseRegex.Match(innerLine);
                if (caseMatch.Success)
                {
                    var weight = ParseCaseWeight(caseMatch.Groups["weight"].Value);

                    currentCase = new RandomCaseBlock
                    {
                        Weight = weight
                    };

                    cases.Add(currentCase);
                    continue;
                }

                currentCase?.Lines.Add(lines[i]);
            }

            if (cases.Count == 0)
            {
                continue;
            }

            var selectedCase = PickWeightedCase(cases);
            result.AddRange(selectedCase.Lines);
        }

        return result.ToArray();
    }

    private static double ParseCaseWeight(string weightText)
    {
        if (string.IsNullOrWhiteSpace(weightText))
        {
            return 1;
        }

        weightText = weightText.Replace(',', '.');

        if (!double.TryParse(weightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight))
        {
            return 1;
        }

        return weight > 0 ? weight : 1;
    }

    private static RandomCaseBlock PickWeightedCase(IReadOnlyList<RandomCaseBlock> cases)
    {
        var totalWeight = cases.Sum(x => x.Weight);

        if (totalWeight <= 0)
        {
            return cases[Random.Next(cases.Count)];
        }

        var roll = Random.NextDouble() * totalWeight;
        var cumulative = 0d;

        foreach (var item in cases)
        {
            cumulative += item.Weight;

            if (roll <= cumulative)
            {
                return item;
            }
        }

        return cases[^1];
    }

    private static bool TryParseWait(string line, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;

        var match = WaitRegex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        var rawValue = match.Groups["value"].Value.Replace(',', '.');

        if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            value < 0)
        {
            return false;
        }

        var unit = match.Groups["unit"].Value.ToLowerInvariant();

        delay = unit switch
        {
            "ms" => TimeSpan.FromMilliseconds(value),
            "m" => TimeSpan.FromMinutes(value),
            "h" => TimeSpan.FromHours(value),
            _ => TimeSpan.FromSeconds(value)
        };

        return true;
    }

    private static string ReplacePlaceholders(string text, ChatCommand chatCommand, string? playerLocation)
    {
        var now = DateTime.Now;
        var args = string.Join(" ", chatCommand.Arguments);

        var result = text
            .Replace("{player}", chatCommand.PlayerName, StringComparison.OrdinalIgnoreCase)
            .Replace("{steamId}", chatCommand.SteamId, StringComparison.OrdinalIgnoreCase)
            .Replace("{command}", chatCommand.Command, StringComparison.OrdinalIgnoreCase)
            .Replace("{args}", args, StringComparison.OrdinalIgnoreCase)
            .Replace("{now}", now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{date}", now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", now.ToString("HH:mm:ss", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        result = ReplacePlayerLocationPlaceholders(result, playerLocation);

        for (var i = 0; i < chatCommand.Arguments.Count; i++)
        {
            result = result.Replace(
                $"{{arg{i + 1}}}",
                chatCommand.Arguments[i],
                StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static string ReplacePlayerLocationPlaceholders(string text, string? playerLocation)
    {
        return PlayerLocationPlaceholderRegex.Replace(text, match =>
        {
            if (string.IsNullOrWhiteSpace(playerLocation))
            {
                return string.Empty;
            }

            var offsetText = match.Groups["offset"].Value;

            if (string.IsNullOrWhiteSpace(offsetText))
            {
                return playerLocation;
            }

            if (!TryParseLocation(playerLocation, out var x, out var y, out var z))
            {
                return playerLocation;
            }

            if (!double.TryParse(offsetText, NumberStyles.Float, CultureInfo.InvariantCulture, out var zOffset))
            {
                return playerLocation;
            }

            z += zOffset;

            return string.Format(
                CultureInfo.InvariantCulture,
                "\"[{0:0.###} {1:0.###} {2:0.###}]\"",
                x,
                y,
                z);
        });
    }

    private static bool TryParseLocation(string location, out double x, out double y, out double z)
    {
        x = 0;
        y = 0;
        z = 0;

        var namedMatch = Regex.Match(
            location,
            @"X\s*=\s*(?<x>-?\d+(?:\.\d+)?)\s+Y\s*=\s*(?<y>-?\d+(?:\.\d+)?)\s+Z\s*=\s*(?<z>-?\d+(?:\.\d+)?)",
            RegexOptions.IgnoreCase);

        if (namedMatch.Success)
        {
            return double.TryParse(namedMatch.Groups["x"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                   && double.TryParse(namedMatch.Groups["y"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                   && double.TryParse(namedMatch.Groups["z"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out z);
        }

        var rawMatch = Regex.Match(
            location.Trim(),
            "^\"?\\[?\\s*(?<x>-?\\d+(?:\\.\\d+)?)\\s+(?<y>-?\\d+(?:\\.\\d+)?)\\s+(?<z>-?\\d+(?:\\.\\d+)?)\\s*\\]?\"?$",
            RegexOptions.IgnoreCase);

        if (!rawMatch.Success)
        {
            return false;
        }

        return double.TryParse(rawMatch.Groups["x"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x)
               && double.TryParse(rawMatch.Groups["y"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y)
               && double.TryParse(rawMatch.Groups["z"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out z);
    }
}