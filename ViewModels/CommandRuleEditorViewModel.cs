using ScumFreeBot.Models;

namespace ScumFreeBot.ViewModels;

public sealed class CommandRuleEditorViewModel : ObservableObject
{
    private bool _enabled = true;
    private string _trigger = string.Empty;
    private string _scriptFile = string.Empty;
    private string _runMode = "Always";
    private double _cooldownHours;
    private string _zone = string.Empty;
    private string _denyMessage = string.Empty;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (SetProperty(ref _enabled, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string Trigger
    {
        get => _trigger;
        set
        {
            if (SetProperty(ref _trigger, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string ScriptFile
    {
        get => _scriptFile;
        set
        {
            if (SetProperty(ref _scriptFile, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string RunMode
    {
        get => _runMode;
        set => SetProperty(ref _runMode, NormalizeDisplayRunMode(value));
    }

    public double CooldownHours
    {
        get => _cooldownHours;
        set => SetProperty(ref _cooldownHours, value < 0 ? 0 : value);
    }

    public string Zone
    {
        get => _zone;
        set => SetProperty(ref _zone, value);
    }

    public string DenyMessage
    {
        get => _denyMessage;
        set => SetProperty(ref _denyMessage, value);
    }

    public string DisplayName
    {
        get
        {
            var trigger = string.IsNullOrWhiteSpace(Trigger) ? "Neuer Befehl" : Trigger;
            return Enabled ? trigger : $"{trigger} (aus)";
        }
    }

    public static CommandRuleEditorViewModel FromModel(CommandRule rule)
    {
        return new CommandRuleEditorViewModel
        {
            Enabled = rule.Enabled,
            Trigger = rule.Trigger,
            ScriptFile = rule.ScriptFile,
            RunMode = ToDisplayRunMode(rule.RunMode),
            CooldownHours = rule.CooldownHours,
            Zone = rule.Zone,
            DenyMessage = rule.DenyMessage
        };
    }

    private static string NormalizeDisplayRunMode(string? mode)
    {
        return (mode ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .ToLowerInvariant() switch
        {
            "uniqueperplayer" or "unique" or "once" or "einzigartigprospieler"
                => "Einzigartig pro Spieler",

            "dailyperplayer" or "daily" or "1xtaeglichprospieler" or "1xtaglichprospieler"
                => "1x taeglich pro Spieler",

            "hours" or "stunden" or "zeitstunden" or "zeit(stunden)"
                => "Zeit (Stunden)",

            "always" or "immer"
                => "Immer",

            _ => "Immer"
        };
    }

    public CommandRule ToModel()
    {
        return new CommandRule
        {
            Enabled = Enabled,
            Trigger = Trigger?.Trim() ?? string.Empty,
            ScriptFile = ScriptFile?.Trim() ?? string.Empty,
            RunMode = ToStoredRunMode(RunMode),
            CooldownHours = CooldownHours,
            Zone = Zone?.Trim() ?? string.Empty,
            DenyMessage = DenyMessage ?? string.Empty
        };
    }
    private static string ToDisplayRunMode(string mode)
    {
        return NormalizeDisplayRunMode(mode);
    }

    private static string ToStoredRunMode(string mode)
    {
        return (mode ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .ToLowerInvariant() switch
        {
            "einzigartigprospieler" or "uniqueperplayer" => "UniquePerPlayer",
            "1xtaeglichprospieler" or "1xtaglichprospieler" or "dailyperplayer" => "DailyPerPlayer",
            "zeit(stunden)" or "zeitstunden" or "hours" or "stunden" => "Hours",
            _ => "Always"
        };
    }

}
