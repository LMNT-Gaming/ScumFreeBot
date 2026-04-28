using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class CommandCenterService
{
    private readonly CommandCenterConfigService _configService;
    private readonly PlayerStateStore _playerStateStore;
    private readonly CommandScriptRunnerService _scriptRunnerService;
    private readonly CommandSenderService _commandSenderService;
    private readonly HashSet<string> _runningKeys = new(StringComparer.OrdinalIgnoreCase);

    public CommandCenterService(
        CommandCenterConfigService configService,
        PlayerStateStore playerStateStore,
        CommandScriptRunnerService scriptRunnerService,
        CommandSenderService commandSenderService)
    {
        _configService = configService;
        _playerStateStore = playerStateStore;
        _scriptRunnerService = scriptRunnerService;
        _commandSenderService = commandSenderService;
    }

    public async Task<CommandCenterHandleResult> HandleAsync(string autoHotkeyPath, string ahkScriptPath, ChatCommand chatCommand)
    {
        var config = _configService.Load();
        var rule = config.Commands.FirstOrDefault(x =>
            x.Enabled && string.Equals(x.Trigger, chatCommand.Command, StringComparison.OrdinalIgnoreCase));

        if (rule is null)
        {
            return CommandCenterHandleResult.NotHandled();
        }

        var playerKey = GetPlayerKey(chatCommand);
        var triggerKey = NormalizeTrigger(rule.Trigger);

        var runningKey = $"{playerKey}:{triggerKey}";
        lock (_runningKeys)
        {
            if (!_runningKeys.Add(runningKey))
            {
                return CommandCenterHandleResult.Handled($"{rule.Trigger} laeuft fuer {chatCommand.PlayerName} bereits.");
            }
        }

        try
        {
            var allowed = IsAllowed(rule, playerKey, triggerKey, out var reason);
            if (!allowed)
            {
                var message = string.IsNullOrWhiteSpace(rule.DenyMessage)
                    ? reason
                    : ReplaceBasicPlaceholders(rule.DenyMessage, chatCommand);

                await _commandSenderService.SendCommandAsync(autoHotkeyPath, ahkScriptPath, message);
                return CommandCenterHandleResult.Handled(reason);
            }

            // Bei Einmal-/Cooldown-Regeln sofort reservieren, damit der Befehl nicht erneut durchrutscht,
            // falls das Script lange läuft oder der Spieler mehrfach spammt.
            _playerStateStore.MarkCommandUsed(playerKey, chatCommand.PlayerName, triggerKey);

            await _scriptRunnerService.RunAsync(autoHotkeyPath, ahkScriptPath, rule, chatCommand);
            return CommandCenterHandleResult.Handled($"{rule.Trigger} fuer {chatCommand.PlayerName} ausgefuehrt.");
        }
        finally
        {
            lock (_runningKeys)
            {
                _runningKeys.Remove(runningKey);
            }
        }
    }

    private bool IsAllowed(CommandRule rule, string playerKey, string triggerKey, out string reason)
    {
        reason = string.Empty;
        var mode = NormalizeMode(rule.RunMode);

        if (mode == "always")
        {
            return true;
        }

        var state = _playerStateStore.GetCommandState(playerKey, triggerKey);

        // Wichtig für Migration vom alten Welcomepack-System:
        // Wenn der Spieler früher schon ein Welcomepack bekommen hat,
        // soll !welcomepack bei UniquePerPlayer ebenfalls gesperrt sein.
        if (mode == "uniqueperplayer" &&
            string.Equals(triggerKey, "!welcomepack", StringComparison.OrdinalIgnoreCase) &&
            _playerStateStore.HasReceivedWelcomePack(playerKey))
        {
            reason = "Befehl ist pro Spieler nur einmal erlaubt.";
            return false;
        }

        if (state is null || state.LastUsedAtUtc is null)
        {
            return true;
        }

        var lastUsedUtc = DateTime.SpecifyKind(state.LastUsedAtUtc.Value, DateTimeKind.Utc);
        var nowUtc = DateTime.UtcNow;

        if (mode == "uniqueperplayer")
        {
            reason = "Befehl ist pro Spieler nur einmal erlaubt.";
            return false;
        }

        if (mode == "dailyperplayer")
        {
            var lastLocalDate = lastUsedUtc.ToLocalTime().Date;
            var todayLocalDate = DateTime.Now.Date;

            if (lastLocalDate == todayLocalDate)
            {
                reason = "Befehl ist fuer diesen Spieler heute bereits benutzt worden.";
                return false;
            }

            return true;
        }

        if (mode == "hours")
        {
            var cooldownHours = rule.CooldownHours <= 0 ? 1 : rule.CooldownHours;
            var cooldown = TimeSpan.FromHours(cooldownHours);
            var elapsed = nowUtc - lastUsedUtc;

            if (elapsed < cooldown)
            {
                var remaining = cooldown - elapsed;

                reason = remaining.TotalHours >= 1
                    ? $"Befehl ist noch {Math.Ceiling(remaining.TotalHours)} Stunde(n) gesperrt."
                    : $"Befehl ist noch {Math.Ceiling(remaining.TotalMinutes)} Minute(n) gesperrt.";

                return false;
            }

            return true;
        }

        return true;
    }

    private static string GetPlayerKey(ChatCommand chatCommand)
    {
        if (!string.IsNullOrWhiteSpace(chatCommand.SteamId))
        {
            return chatCommand.SteamId.Trim();
        }

        // Fallback, falls im Log keine SteamId sauber erkannt wurde.
        return chatCommand.PlayerName.Trim().ToLowerInvariant();
    }

    private static string NormalizeTrigger(string trigger)
    {
        return (trigger ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string NormalizeMode(string mode)
    {
        return (mode ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty)
            .ToLowerInvariant() switch
        {
            "uniqueperplayer" => "uniqueperplayer",
            "einzigartigprospieler" => "uniqueperplayer",
            "unique" => "uniqueperplayer",
            "once" => "uniqueperplayer",

            "dailyperplayer" => "dailyperplayer",
            "1xtaeglichprospieler" => "dailyperplayer",
            "1xtaglichprospieler" => "dailyperplayer",
            "daily" => "dailyperplayer",

            "hours" => "hours",
            "zeitstunden" => "hours",
            "stunden" => "hours",

            "always" => "always",
            "immer" => "always",

            _ => "always"
        };
    }

    private static string ReplaceBasicPlaceholders(string text, ChatCommand chatCommand)
    {
        return text
            .Replace("{player}", chatCommand.PlayerName, StringComparison.OrdinalIgnoreCase)
            .Replace("{steamId}", chatCommand.SteamId, StringComparison.OrdinalIgnoreCase)
            .Replace("{command}", chatCommand.Command, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class CommandCenterHandleResult
{
    public bool WasHandled { get; init; }
    public string Message { get; init; } = string.Empty;

    public static CommandCenterHandleResult NotHandled() => new() { WasHandled = false };

    public static CommandCenterHandleResult Handled(string message) => new()
    {
        WasHandled = true,
        Message = message
    };
}
