using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScumFreeBot.Models;
using ScumFreeBot.Services;
using System.Threading.Tasks;
using System.Windows;

namespace ScumFreeBot.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly StatusMonitorService _statusMonitorService;
    private readonly CommandSenderService _commandSenderService;

    private string _autoHotkeyPath = string.Empty;
    private string _scriptPath = string.Empty;
    private string _chatLogDirectory = string.Empty;
    private bool _autoRefreshEnabled = true;
    private int _refreshIntervalSeconds = 2;
    private string _testCommand = "#ListPlayers";

    private string _scumStatusText = "Noch nicht geprüft";
    private System.Windows.Media.Brush _scumStatusBackground = CreateBrush("#64748B");
    private string _scumDetails = "SCUM-Status wurde noch nicht abgefragt.";

    private string _ahkStatusText = "Noch nicht geprüft";
    private System.Windows.Media.Brush _ahkStatusBackground = CreateBrush("#64748B");
    private string _ahkDetails = "AutoHotkey-Pfad wurde noch nicht geprüft.";
    private string _scriptDetails = "Script-Pfad wurde noch nicht geprüft.";

    private string _lastRefreshText = "Letzter Check: noch keiner";

    private string _commandStatusText = "Noch kein Testbefehl gesendet";
    private System.Windows.Media.Brush _commandStatusBackground = CreateBrush("#64748B");
    private string _commandDetails = "Hier erscheint später das Ergebnis vom Testbefehl.";
    private string _chatLogStatusText = "Noch nicht geprüft";
    private System.Windows.Media.Brush _chatLogStatusBackground = CreateBrush("#64748B");
    private string _chatLogDetails = "Noch kein Chatlog geprüft.";
    private string _chatLogPreview = "Noch kein Chatlog geladen.";
    private bool _isSendingCommand;
    private readonly ChatLogMonitorService _chatLogMonitorService = new();
    private readonly ChatCommandParser _chatCommandParser = new();
    private readonly CommandCenterConfigService _commandCenterConfigService = new();
    private readonly CommandCenterService _commandCenterService;
    public bool IsRunningAsAdministrator => AdminPrivilegeService.IsRunningAsAdministrator();

    public bool ShowAdminWarning => !IsRunningAsAdministrator;

    private string _botStatusText = "Noch keine Chat-Commands verarbeitet.";

    private readonly RemoteLogCoordinatorService _remoteLogCoordinatorService = new();

    private string _logSourceMode = "Local";
    private bool _remoteSyncEnabled;
    private string _remoteHost = string.Empty;
    private int _remotePort = 22;
    private string _remoteUsername = string.Empty;
    private string _remotePassword = string.Empty;
    private string _remoteLogsPath = "/SCUM/Saved/SaveFiles/Logs";
    private int _remoteSyncIntervalSeconds = 5;

    private string _remoteSyncStatusText = "Noch kein Remote-Sync ausgeführt.";
    private System.Windows.Media.Brush _remoteSyncStatusBackground = CreateBrush("#64748B");
    private string _remoteSyncDetails = "Hier erscheint das Ergebnis vom manuellen FTP/SFTP-Sync.";
    private bool _isRemoteSyncRunning;

    private CommandRuleEditorViewModel? _selectedCommandRule;
    private string _selectedCommandScriptText = string.Empty;
    private string _commandCenterStatusText = "Noch nicht gespeichert.";

    private readonly PlayerStateStore _playerStateStore = new();
    private PlayerCommandUsageViewModel? _selectedPlayerCommandUsage;
    private string _playerCommandUsageStatusText = "Noch nicht geladen.";

    public ObservableCollection<CommandRuleEditorViewModel> CommandRules { get; } = new();
    public ObservableCollection<PlayerCommandUsageViewModel> PlayerCommandUsages { get; } = new();

    private const string LatestVersionUrl = "https://lmnt-gaming.net/scum-freebot/latest.json";

    private string _currentVersionText = UpdateService.GetCurrentVersionText();
    private string _latestVersionText = "-";
    private string _updateStatusText = "Versionsprüfung noch nicht ausgeführt.";
    private string? _updateDownloadUrl;
    private string? _updatePatchNotesUrl;
    private bool _isUpdateAvailable;
    private bool _isCheckingForUpdates;

    public string[] RunModeOptions { get; } =
    {
        "Einzigartig pro Spieler",
        "1x taeglich pro Spieler",
        "Zeit (Stunden)",
        "Immer"
    };

    public MainViewModel(
        SettingsService settingsService,
        StatusMonitorService statusMonitorService,
        CommandSenderService commandSenderService)
    {
        _settingsService = settingsService;
        _statusMonitorService = statusMonitorService;
        _commandSenderService = commandSenderService;
        _commandCenterService = new CommandCenterService(
    _commandCenterConfigService,
    _playerStateStore,
    new CommandScriptRunnerService(
        commandSenderService,
        new PlayerLocationService()),
    commandSenderService);
    }

    public string BotStatusText
    {
        get => _botStatusText;
        private set => SetProperty(ref _botStatusText, value);
    }

    public CommandRuleEditorViewModel? SelectedCommandRule
    {
        get => _selectedCommandRule;
        set
        {
            if (ReferenceEquals(_selectedCommandRule, value))
            {
                return;
            }

            _selectedCommandRule = value;
            OnPropertyChanged();
            LoadSelectedCommandScript();
        }
    }

    public string CurrentVersionText
    {
        get => _currentVersionText;
        private set => SetProperty(ref _currentVersionText, value);
    }

    public string LatestVersionText
    {
        get => _latestVersionText;
        private set => SetProperty(ref _latestVersionText, value);
    }

    public string UpdateStatusText
    {
        get => _updateStatusText;
        private set => SetProperty(ref _updateStatusText, value);
    }

    public string? UpdateDownloadUrl
    {
        get => _updateDownloadUrl;
        private set => SetProperty(ref _updateDownloadUrl, value);
    }

    public string? UpdatePatchNotesUrl
    {
        get => _updatePatchNotesUrl;
        private set => SetProperty(ref _updatePatchNotesUrl, value);
    }

    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        private set
        {
            if (SetProperty(ref _isUpdateAvailable, value))
            {
                OnPropertyChanged(nameof(UpdateAvailableVisibility));
                OnPropertyChanged(nameof(NoUpdateAvailableVisibility));
            }
        }
    }

    public bool IsCheckingForUpdates
    {
        get => _isCheckingForUpdates;
        private set => SetProperty(ref _isCheckingForUpdates, value);
    }

    public Visibility UpdateAvailableVisibility =>
        IsUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NoUpdateAvailableVisibility =>
        IsUpdateAvailable ? Visibility.Collapsed : Visibility.Visible;

    public string SelectedCommandScriptText
    {
        get => _selectedCommandScriptText;
        set => SetProperty(ref _selectedCommandScriptText, value);
    }

    public string CommandCenterStatusText
    {
        get => _commandCenterStatusText;
        private set => SetProperty(ref _commandCenterStatusText, value);
    }

    public string AutoHotkeyPath
    {
        get => _autoHotkeyPath;
        set => SetProperty(ref _autoHotkeyPath, value);
    }

    public string LogSourceMode
    {
        get => _logSourceMode;
        set => SetProperty(ref _logSourceMode, value);
    }

    public void RefreshAdminState()
    {
        OnPropertyChanged(nameof(IsRunningAsAdministrator));
        OnPropertyChanged(nameof(ShowAdminWarning));
    }

    public bool RemoteSyncEnabled
    {
        get => _remoteSyncEnabled;
        set => SetProperty(ref _remoteSyncEnabled, value);
    }

    public string RemoteHost
    {
        get => _remoteHost;
        set => SetProperty(ref _remoteHost, value);
    }

    public int RemotePort
    {
        get => _remotePort;
        set => SetProperty(ref _remotePort, value);
    }

    public string RemoteUsername
    {
        get => _remoteUsername;
        set => SetProperty(ref _remoteUsername, value);
    }

    public string RemotePassword
    {
        get => _remotePassword;
        set => SetProperty(ref _remotePassword, value);
    }

    public string RemoteLogsPath
    {
        get => _remoteLogsPath;
        set => SetProperty(ref _remoteLogsPath, value);
    }

    public int RemoteSyncIntervalSeconds
    {
        get => _remoteSyncIntervalSeconds;
        set
        {
            var normalized = value < 1 ? 1 : value;
            SetProperty(ref _remoteSyncIntervalSeconds, normalized);
        }
    }

    public string RemoteSyncStatusText
    {
        get => _remoteSyncStatusText;
        private set => SetProperty(ref _remoteSyncStatusText, value);
    }

    public System.Windows.Media.Brush RemoteSyncStatusBackground
    {
        get => _remoteSyncStatusBackground;
        private set => SetProperty(ref _remoteSyncStatusBackground, value);
    }

    public string RemoteSyncDetails
    {
        get => _remoteSyncDetails;
        private set => SetProperty(ref _remoteSyncDetails, value);
    }

    public bool IsRemoteSyncRunning
    {
        get => _isRemoteSyncRunning;
        private set => SetProperty(ref _isRemoteSyncRunning, value);
    }

    public string ScriptPath
    {
        get => _scriptPath;
        set => SetProperty(ref _scriptPath, value);
    }

    public string ChatLogDirectory
    {
        get => _chatLogDirectory;
        set => SetProperty(ref _chatLogDirectory, value);
    }

    public string ChatLogStatusText
    {
        get => _chatLogStatusText;
        private set => SetProperty(ref _chatLogStatusText, value);
    }

    public System.Windows.Media.Brush ChatLogStatusBackground
    {
        get => _chatLogStatusBackground;
        private set => SetProperty(ref _chatLogStatusBackground, value);
    }

    public string ChatLogDetails
    {
        get => _chatLogDetails;
        private set => SetProperty(ref _chatLogDetails, value);
    }

    public string ChatLogPreview
    {
        get => _chatLogPreview;
        private set => SetProperty(ref _chatLogPreview, value);
    }

    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        set => SetProperty(ref _autoRefreshEnabled, value);
    }

    public int RefreshIntervalSeconds
    {
        get => _refreshIntervalSeconds;
        set
        {
            var normalized = value < 1 ? 1 : value;
            SetProperty(ref _refreshIntervalSeconds, normalized);
        }
    }

    public string TestCommand
    {
        get => _testCommand;
        set => SetProperty(ref _testCommand, value);
    }

    public string ScumStatusText
    {
        get => _scumStatusText;
        private set => SetProperty(ref _scumStatusText, value);
    }

    public System.Windows.Media.Brush ScumStatusBackground
    {
        get => _scumStatusBackground;
        private set => SetProperty(ref _scumStatusBackground, value);
    }

    public string ScumDetails
    {
        get => _scumDetails;
        private set => SetProperty(ref _scumDetails, value);
    }

    public string AhkStatusText
    {
        get => _ahkStatusText;
        private set => SetProperty(ref _ahkStatusText, value);
    }

    public System.Windows.Media.Brush AhkStatusBackground
    {
        get => _ahkStatusBackground;
        private set => SetProperty(ref _ahkStatusBackground, value);
    }

    public string AhkDetails
    {
        get => _ahkDetails;
        private set => SetProperty(ref _ahkDetails, value);
    }

    public string ScriptDetails
    {
        get => _scriptDetails;
        private set => SetProperty(ref _scriptDetails, value);
    }

    public string LastRefreshText
    {
        get => _lastRefreshText;
        private set => SetProperty(ref _lastRefreshText, value);
    }

    public string CommandStatusText
    {
        get => _commandStatusText;
        private set => SetProperty(ref _commandStatusText, value);
    }

    public System.Windows.Media.Brush CommandStatusBackground
    {
        get => _commandStatusBackground;
        private set => SetProperty(ref _commandStatusBackground, value);
    }

    public string CommandDetails
    {
        get => _commandDetails;
        private set => SetProperty(ref _commandDetails, value);
    }

    public bool IsSendingCommand
    {
        get => _isSendingCommand;
        private set => SetProperty(ref _isSendingCommand, value);
    }

    public PlayerCommandUsageViewModel? SelectedPlayerCommandUsage
    {
        get => _selectedPlayerCommandUsage;
        set => SetProperty(ref _selectedPlayerCommandUsage, value);
    }

    public string PlayerCommandUsageStatusText
    {
        get => _playerCommandUsageStatusText;
        private set => SetProperty(ref _playerCommandUsageStatusText, value);
    }

    public void LoadSettings()
    {
        var settings = _settingsService.Load();
        AutoHotkeyPath = settings.AutoHotkeyPath;
        ScriptPath = settings.ScriptPath;
        ChatLogDirectory = settings.ChatLogDirectory;
        AutoRefreshEnabled = settings.AutoRefreshEnabled;
        RefreshIntervalSeconds = settings.RefreshIntervalSeconds;
        TestCommand = settings.TestCommand;
        LogSourceMode = settings.LogSourceMode;
        RemoteSyncEnabled = settings.RemoteSyncEnabled;
        RemoteHost = settings.RemoteHost;
        RemotePort = settings.RemotePort;
        RemoteUsername = settings.RemoteUsername;
        RemotePassword = settings.RemotePassword;
        RemoteLogsPath = settings.RemoteLogsPath;
        RemoteSyncIntervalSeconds = settings.RemoteSyncIntervalSeconds;
        LoadCommandCenter();
        LoadPlayerCommandUsages();
    }

    public void LoadCommandCenter()
    {
        var config = _commandCenterConfigService.Load();
        CommandRules.Clear();

        foreach (var rule in config.Commands)
        {
            CommandRules.Add(CommandRuleEditorViewModel.FromModel(rule));
        }

        SelectedCommandRule = CommandRules.FirstOrDefault();
        CommandCenterStatusText = $"{CommandRules.Count} Befehl(e) geladen.";
    }

    public void AddCommandRule()
    {
        var number = CommandRules.Count + 1;
        var rule = new CommandRuleEditorViewModel
        {
            Enabled = true,
            Trigger = $"!befehl{number}",
            ScriptFile = $"befehl{number}.sfb",
            RunMode = "Immer",
            CooldownHours = 24,
            DenyMessage = "{player} du kannst diesen Befehl aktuell nicht benutzen."
        };

        CommandRules.Add(rule);
        SelectedCommandRule = rule;
        SelectedCommandScriptText = string.Join(Environment.NewLine, new[]
        {
            $"# Ablaufscript fuer {rule.Trigger}",
            "# Platzhalter: {player}, {steamId}, {arg1}, {arg2}, ...",
            "say {player} Befehl wird ausgefuehrt.",
            "wait 1s"
        });
        CommandCenterStatusText = "Neuer Befehl angelegt. Bitte speichern.";
    }

    public void DeleteSelectedCommandRule()
    {
        if (SelectedCommandRule is null)
        {
            return;
        }

        var removed = SelectedCommandRule;
        var index = CommandRules.IndexOf(removed);
        CommandRules.Remove(removed);
        SelectedCommandRule = CommandRules.Count == 0
            ? null
            : CommandRules[Math.Clamp(index, 0, CommandRules.Count - 1)];
        CommandCenterStatusText = $"{removed.Trigger} entfernt. Bitte speichern.";
    }

    public void SaveCommandCenter()
    {
        SaveSelectedCommandScript();

        var config = new CommandCenterConfig
        {
            Commands = CommandRules.Select(x => x.ToModel()).ToList()
        };

        _commandCenterConfigService.Save(config);
        CommandCenterStatusText = $"Steuerungszentrale gespeichert: {CommandRules.Count} Befehl(e).";
    }

    public void LoadPlayerCommandUsages()
    {
        PlayerCommandUsages.Clear();

        var usages = _playerStateStore.GetAllCommandStates();

        foreach (var usage in usages)
        {
            PlayerCommandUsages.Add(new PlayerCommandUsageViewModel
            {
                PlayerKey = usage.PlayerKey,
                PlayerName = usage.PlayerName,
                Trigger = usage.Trigger,
                UseCount = usage.CommandState.UseCount,
                LastUsedAtUtc = usage.CommandState.LastUsedAtUtc
            });
        }

        SelectedPlayerCommandUsage = PlayerCommandUsages.FirstOrDefault();

        PlayerCommandUsageStatusText = PlayerCommandUsages.Count == 0
            ? "Noch keine ausgeführten Spielerbefehle gespeichert."
            : $"{PlayerCommandUsages.Count} ausgeführte Spielerbefehle geladen.";
    }

    public void ResetSelectedPlayerCommandUsage()
    {
        if (SelectedPlayerCommandUsage is null)
        {
            PlayerCommandUsageStatusText = "Kein Eintrag ausgewählt.";
            return;
        }

        var player = SelectedPlayerCommandUsage.DisplayPlayerName;
        var trigger = SelectedPlayerCommandUsage.Trigger;

        _playerStateStore.ResetCommandState(
            SelectedPlayerCommandUsage.PlayerKey,
            SelectedPlayerCommandUsage.Trigger);

        LoadPlayerCommandUsages();

        PlayerCommandUsageStatusText = $"{trigger} für {player} wurde zurückgesetzt.";
    }

    public void ResetAllCommandUsagesForSelectedPlayer()
    {
        if (SelectedPlayerCommandUsage is null)
        {
            PlayerCommandUsageStatusText = "Kein Spieler ausgewählt.";
            return;
        }

        var player = SelectedPlayerCommandUsage.DisplayPlayerName;

        _playerStateStore.ResetAllCommandStatesForPlayer(
            SelectedPlayerCommandUsage.PlayerKey);

        LoadPlayerCommandUsages();

        PlayerCommandUsageStatusText = $"Alle Befehlsausführungen für {player} wurden zurückgesetzt.";
    }

    private void LoadSelectedCommandScript()
    {
        if (SelectedCommandRule is null || string.IsNullOrWhiteSpace(SelectedCommandRule.ScriptFile))
        {
            SelectedCommandScriptText = string.Empty;
            return;
        }

        var scriptPath = Path.Combine(AppPaths.DataDirectory, SelectedCommandRule.ScriptFile);
        SelectedCommandScriptText = File.Exists(scriptPath)
            ? File.ReadAllText(scriptPath)
            : string.Join(Environment.NewLine, new[]
            {
                $"# Ablaufscript fuer {SelectedCommandRule.Trigger}",
                "# wait Beispiele: wait 500ms, wait 1s, wait 2m",
                "say {player} Befehl wird ausgefuehrt."
            });
    }

    private void SaveSelectedCommandScript()
    {
        if (SelectedCommandRule is null)
        {
            return;
        }

        Directory.CreateDirectory(AppPaths.DataDirectory);

        if (string.IsNullOrWhiteSpace(SelectedCommandRule.ScriptFile))
        {
            var cleanTrigger = new string((SelectedCommandRule.Trigger ?? "befehl")
                .Where(char.IsLetterOrDigit)
                .ToArray());

            if (string.IsNullOrWhiteSpace(cleanTrigger))
            {
                cleanTrigger = "befehl";
            }

            SelectedCommandRule.ScriptFile = $"{cleanTrigger.ToLowerInvariant()}.sfb";
        }

        var scriptPath = Path.Combine(AppPaths.DataDirectory, SelectedCommandRule.ScriptFile);
        File.WriteAllText(scriptPath, SelectedCommandScriptText ?? string.Empty);
    }

    public void SaveSettings()
    {
        var settings = new AppSettings
        {
            AutoHotkeyPath = AutoHotkeyPath,
            ScriptPath = ScriptPath,
            ChatLogDirectory = ChatLogDirectory,
            AutoRefreshEnabled = AutoRefreshEnabled,
            RefreshIntervalSeconds = RefreshIntervalSeconds,
            TestCommand = TestCommand,
            LogSourceMode = LogSourceMode,
            RemoteSyncEnabled = RemoteSyncEnabled,
            RemoteHost = RemoteHost,
            RemotePort = RemotePort,
            RemoteUsername = RemoteUsername,
            RemotePassword = RemotePassword,
            RemoteLogsPath = RemoteLogsPath,
            RemoteSyncIntervalSeconds = RemoteSyncIntervalSeconds,
        };

        _settingsService.Save(settings);
    }

    public async Task RunRemoteSyncAsync()
    {
        if (IsRemoteSyncRunning)
        {
            return;
        }

        IsRemoteSyncRunning = true;
        RemoteSyncStatusText = "Synchronisiere...";
        RemoteSyncStatusBackground = CreateBrush("#2563EB");
        RemoteSyncDetails = "Remote-Chatlog wird geladen.";

        try
        {
            var result = await _remoteLogCoordinatorService.SyncAsync(new RemoteLogSettings
            {
                LogSourceMode = LogSourceMode,
                RemoteHost = RemoteHost,
                RemotePort = RemotePort,
                RemoteUsername = RemoteUsername,
                RemotePassword = RemotePassword,
                RemoteLogsPath = RemoteLogsPath
            });

            if (result.Success)
            {
                RemoteSyncStatusText = "Sync erfolgreich";
                RemoteSyncStatusBackground = CreateBrush("#16A34A");
                RemoteSyncDetails = $"{result.Message}{Environment.NewLine}Lokale Datei: {result.LocalFilePath}";
            }
            else
            {
                RemoteSyncStatusText = "Sync fehlgeschlagen";
                RemoteSyncStatusBackground = CreateBrush("#DC2626");
                RemoteSyncDetails = result.Message;
            }
        }
        finally
        {
            IsRemoteSyncRunning = false;
        }
    }

    public async Task RefreshAsync()
    {

        var effectiveChatLogDirectory =
    string.Equals(LogSourceMode, "Local", StringComparison.OrdinalIgnoreCase)
        ? ChatLogDirectory
        : AppPaths.RemoteLogsDirectory;

        var snapshot = await _statusMonitorService.CheckAsync(
    AutoHotkeyPath,
    ScriptPath,
    effectiveChatLogDirectory);

        if (snapshot.IsScumRunning)
        {
            ScumStatusText = "SCUM läuft";
            ScumStatusBackground = CreateBrush("#16A34A");
            ScumDetails = $"SCUM wurde erkannt. Aktive Prozesse: {snapshot.ScumProcessCount}.";
        }
        else
        {
            ScumStatusText = "SCUM nicht gefunden";
            ScumStatusBackground = CreateBrush("#DC2626");
            ScumDetails = "Es läuft aktuell kein SCUM-Prozess mit dem Namen SCUM.exe.";
        }

        if (snapshot.IsAutoHotkeyFound && snapshot.IsScriptFound)
        {
            AhkStatusText = "AutoHotkey bereit";
            AhkStatusBackground = CreateBrush("#16A34A");
        }
        else if (snapshot.IsAutoHotkeyFound || snapshot.IsScriptFound)
        {
            AhkStatusText = "Teilweise bereit";
            AhkStatusBackground = CreateBrush("#D97706");
        }
        else
        {
            AhkStatusText = "Nicht bereit";
            AhkStatusBackground = CreateBrush("#DC2626");
        }

        if (snapshot.IsChatLogAvailable)
        {
            ChatLogStatusText = "Chatlog gefunden";
            ChatLogStatusBackground = CreateBrush("#16A34A");
            ChatLogDetails = $"Neueste Datei: {snapshot.LatestChatLogFile}";
        }
        else
        {
            ChatLogStatusText = "Kein Chatlog gefunden";
            ChatLogStatusBackground = CreateBrush("#DC2626");
            ChatLogDetails = "Im ausgewählten Ordner wurde keine Datei vom Typ chat_YYYYMMDDHHMMSS.log gefunden.";
        }

        ChatLogPreview = snapshot.ChatLogPreview;

        AhkDetails = snapshot.IsAutoHotkeyFound
            ? $"AutoHotkey EXE gefunden: {snapshot.AutoHotkeyPath}"
            : "AutoHotkey EXE wurde unter dem eingestellten Pfad nicht gefunden.";

        ScriptDetails = snapshot.IsScriptFound
            ? $"AHK Script gefunden: {snapshot.ScriptPath}"
            : "AHK Script wurde unter dem eingestellten Pfad nicht gefunden.";

        LastRefreshText = $"Letzter Check: {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)}";

        var newLines = _chatLogMonitorService.ReadNewLines(effectiveChatLogDirectory);

        foreach (var line in newLines)
        {
            var command = _chatCommandParser.TryParse(line);
            if (command is null)
            {
                continue;
            }
            var commandCenterResult = await _commandCenterService.HandleAsync(
                AutoHotkeyPath,
                ScriptPath,
                command);

            if (commandCenterResult.WasHandled)
            {
                BotStatusText = commandCenterResult.Message;
            }
        }
    }

    public async Task SendTestCommandAsync()
    {
        if (IsSendingCommand)
        {
            return;
        }

        IsSendingCommand = true;
        CommandStatusText = "Sende...";
        CommandStatusBackground = CreateBrush("#2563EB");
        CommandDetails = "Testbefehl wird an AutoHotkey übergeben.";

        try
        {
            var result = await _commandSenderService.SendCommandAsync(
                AutoHotkeyPath,
                ScriptPath,
                TestCommand);

            if (result.Success)
            {
                CommandStatusText = "Gesendet";
                CommandStatusBackground = CreateBrush("#16A34A");
                CommandDetails = result.Message;
            }
            else
            {
                CommandStatusText = "Fehler";
                CommandStatusBackground = CreateBrush("#DC2626");
                CommandDetails = result.Message;
            }
        }
        finally
        {
            IsSendingCommand = false;
        }
    }

    private static System.Windows.Media.Brush CreateBrush(string hexColor)
    {
        return new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor));
    }

    public async Task CheckForUpdatesAsync()
    {
        if (IsCheckingForUpdates)
        {
            return;
        }

        IsCheckingForUpdates = true;

        try
        {
            CurrentVersionText = UpdateService.GetCurrentVersionText();
            LatestVersionText = "-";
            UpdateDownloadUrl = null;
            UpdatePatchNotesUrl = null;
            IsUpdateAvailable = false;
            UpdateStatusText = "Prüfe auf Updates...";

            var updateService = new UpdateService();
            var latest = await updateService.GetLatestAsync(LatestVersionUrl);

            if (latest is null || string.IsNullOrWhiteSpace(latest.Version))
            {
                UpdateStatusText = "Keine Versionsinformationen gefunden.";
                return;
            }

            LatestVersionText = latest.Version;
            UpdateDownloadUrl = latest.DownloadUrl;
            UpdatePatchNotesUrl = latest.PatchNotesUrl;

            if (UpdateService.IsNewer(latest.Version))
            {
                IsUpdateAvailable = true;

                UpdateStatusText = latest.Mandatory
                    ? $"Pflichtupdate verfügbar: Version {latest.Version}"
                    : $"Update verfügbar: Version {latest.Version}";

                return;
            }

            UpdateStatusText = $"Du nutzt die aktuelle Version ({CurrentVersionText}).";
        }
        catch (Exception ex)
        {
            IsUpdateAvailable = false;
            LatestVersionText = "-";
            UpdateDownloadUrl = null;
            UpdatePatchNotesUrl = null;
            UpdateStatusText = $"Versionsprüfung fehlgeschlagen: {ex.Message}";
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }
}