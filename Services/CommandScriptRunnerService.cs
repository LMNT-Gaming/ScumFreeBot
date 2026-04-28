using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class CommandScriptRunnerService
{
    private static readonly Regex WaitRegex = new(@"^wait\s+(?<value>\d+(?:[\.,]\d+)?)(?<unit>ms|s|m|h)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly CommandSenderService _commandSenderService;
    private readonly PlayerLocationService _playerLocationService;

    public CommandScriptRunnerService(
    CommandSenderService commandSenderService,
    PlayerLocationService playerLocationService)
    {
        _commandSenderService = commandSenderService;
        _playerLocationService = playerLocationService;
    }

    public async Task RunAsync(string autoHotkeyPath, string ahkScriptPath, CommandRule rule, ChatCommand chatCommand)
    {
        var scriptPath = Path.Combine(AppPaths.DataDirectory, rule.ScriptFile);
        if (!File.Exists(scriptPath))
        {
            await _commandSenderService.SendCommandAsync(autoHotkeyPath, ahkScriptPath, $"Script nicht gefunden: {rule.ScriptFile}");
            return;
        }

        var lines = await File.ReadAllLinesAsync(scriptPath);
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
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("# ") || line.StartsWith("//") || line.StartsWith(";"))
            {
                continue;
            }

            if (TryParseWait(line, out var delay))
            {
                await Task.Delay(delay);
                continue;
            }

            var commandText = ReplacePlaceholders(line, chatCommand, playerLocation);
            await _commandSenderService.SendCommandAsync(autoHotkeyPath, ahkScriptPath, commandText);
        }
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
        if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value < 0)
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
            .Replace("{playerlocation}", playerLocation ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{now}", now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{date}", now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", now.ToString("HH:mm:ss", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        for (var i = 0; i < chatCommand.Arguments.Count; i++)
        {
            result = result.Replace($"{{arg{i + 1}}}", chatCommand.Arguments[i], StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
