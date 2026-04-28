using ScumFreeBot.Services;
using ScumFreeBot.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace ScumFreeBot;

public partial class MainWindow : Window 
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _remoteSyncTimer;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(
            new SettingsService(),
            new StatusMonitorService(),
            new CommandSenderService());

        DataContext = _viewModel;

        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
        _remoteSyncTimer = new DispatcherTimer();
        _remoteSyncTimer.Tick += RemoteSyncTimer_Tick;

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.LoadSettings();
        _viewModel.RefreshAdminState();
        RemotePasswordBox.Password = _viewModel.RemotePassword;
        UpdateTimer();
        UpdateRemoteSyncTimer();
        await _viewModel.RefreshAsync();
    }

    private async void RemoteSyncTimer_Tick(object? sender, EventArgs e)
    {
        if (!_viewModel.RemoteSyncEnabled)
        {
            return;
        }

        if (string.Equals(_viewModel.LogSourceMode, "Local", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _viewModel.RunRemoteSyncAsync();
        await _viewModel.RefreshAsync();
        UpdateRemoteSyncTimer();
    }

    private void UpdateRemoteSyncTimer()
    {
        _remoteSyncTimer.Stop();

        if (!_viewModel.RemoteSyncEnabled)
        {
            return;
        }

        if (string.Equals(_viewModel.LogSourceMode, "Local", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var seconds = Math.Max(1, _viewModel.RemoteSyncIntervalSeconds);
        _remoteSyncTimer.Interval = TimeSpan.FromSeconds(seconds);
        _remoteSyncTimer.Start();
    }

    private async void Timer_Tick(object? sender, EventArgs e)
    {
        if (!_viewModel.AutoRefreshEnabled)
        {
            return;
        }

        await _viewModel.RefreshAsync();
        UpdateTimer();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshAsync();
        UpdateTimer();
        UpdateRemoteSyncTimer();
    }

    private void RestartAsAdminButton_Click(object sender, RoutedEventArgs e)
    {
        AdminPrivilegeService.RestartAsAdministrator();
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveSettings();
        UpdateTimer();
        UpdateRemoteSyncTimer();

        System.Windows.MessageBox.Show(
            this,
            "Die Einstellungen wurden gespeichert.",
            "ScumFreeBot",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void SendTestCommandButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SendTestCommandAsync();
    }

    private void RemotePasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.RemotePassword = passwordBox.Password;
        }
    }
    private async void RunRemoteSyncButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RunRemoteSyncAsync();
        await _viewModel.RefreshAsync();
        UpdateRemoteSyncTimer();
    }

    private void AddCommandRuleButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AddCommandRule();
    }

    private void DeleteCommandRuleButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DeleteSelectedCommandRule();
    }

    private void SaveCommandCenterButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveCommandCenter();

        System.Windows.MessageBox.Show(
            this,
            "Die Steuerungszentrale wurde gespeichert.",
            "ScumFreeBot",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

private void ShowCommandScriptHelpButton_Click(object sender, RoutedEventArgs e)
{
    var dialog = new Window
    {
        Title = "Script-Hilfe",
        Owner = this,
        Width = 760,
        Height = 700,
        MinWidth = 620,
        MinHeight = 520,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Background = GetBrush("WindowBackgroundBrush", "#0F172A"),
        ShowInTaskbar = false
    };

    var panel = new StackPanel
    {
        Margin = new Thickness(22)
    };

    panel.Children.Add(CreateHelpTitle("Script-Hilfe fuer Befehle"));
    panel.Children.Add(CreateHelpText("Hier siehst du die aktuell unterstuetzten Variablen und Ablaufbefehle fuer die .sfb-Skripte in deinem Data-Ordner."));

    panel.Children.Add(CreateHelpHeading("Variablen"));
    panel.Children.Add(CreateHelpCode("{player}   Spielername\n{steamId}  Steam-ID des Spielers\n{command}  Ausgeloester Spielerbefehl, z. B. !vote\n{args}     Alle Argumente nach dem Spielerbefehl\n{arg1}     Erstes Argument\n{arg2}     Zweites Argument\n{arg3}     Drittes Argument\n{now}      Aktuelles Datum mit Uhrzeit\n{date}     Aktuelles Datum\n{time}     Aktuelle Uhrzeit"));

    panel.Children.Add(CreateHelpCode("{playerlocation}   Übergibt die Spielercoordinaten in X Y Z"));

    panel.Children.Add(CreateHelpHeading("Ablaufsteuerung"));
    panel.Children.Add(CreateHelpCode("wait 500ms\nwait 1s\nwait 30s\nwait 2m\nwait 1h"));
    panel.Children.Add(CreateHelpText("wait pausiert den Ablauf an genau dieser Stelle. Dadurch entscheidet der Admin im Skript selbst, wie viel Abstand zwischen Teleport, Spawn und Chatmeldung liegt."));

    panel.Children.Add(CreateHelpHeading("Kommentare"));
    panel.Children.Add(CreateHelpCode("// Kommentar\n# Kommentar\n; Kommentar"));
    panel.Children.Add(CreateHelpText("Wichtig: SCUM-Adminbefehle wie #spawnitem bleiben gueltig. Nur Zeilen mit '# ' also Raute plus Leerzeichen werden als Kommentar ignoriert."));

    panel.Children.Add(CreateHelpHeading("Beispiel: !vote settime 1"));
    panel.Children.Add(CreateHelpCode("Spieler schreibt:\n!vote settime 1\n\nSkript:\n#vote {arg1} {arg2}\n\nErgebnis:\n#vote settime 1"));

    panel.Children.Add(CreateHelpHeading("Beispiel mit allen Argumenten"));
    panel.Children.Add(CreateHelpCode("Spieler schreibt:\n!vote setweather clear\n\nSkript:\n#vote {args}\n\nErgebnis:\n#vote setweather clear"));

    panel.Children.Add(CreateHelpHeading("Welcomepack-Beispiel"));
    panel.Children.Add(CreateHelpCode("{player} dein Welcomepack ist auf dem Weg, bitte bleib stehen!\nwait 1s\n#teleportto {player}\nwait 30s\n#spawnitem BP_Weapon_98k_Kar98 1 Location {player}\nwait 500ms\n#spawnitem BP_Weapon_Magazine_Kar98 2 Location {player}"));

    var closeButton = new WpfButton
    {
        Content = "Schliessen",
        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
        Margin = new Thickness(0, 18, 0, 0),
        Padding = new Thickness(16, 8, 16, 8),
        Background = GetBrush("AccentBrush", "#2563EB"),
        Foreground = WpfBrushes.White,
        BorderBrush = GetBrush("AccentBrush", "#2563EB"),
        BorderThickness = new Thickness(1)
    };
    closeButton.Click += (_, _) => dialog.Close();
    panel.Children.Add(closeButton);

    dialog.Content = new Border
    {
        Background = GetBrush("CardBackgroundBrush", "#111827"),
        BorderBrush = GetBrush("CardBorderBrush", "#1F2937"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(18),
        Margin = new Thickness(18),
        Child = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = panel
        }
    };

    dialog.ShowDialog();
}

private TextBlock CreateHelpTitle(string text)
{
    return new TextBlock
    {
        Text = text,
        Foreground = GetBrush("PrimaryTextBrush", "#F8FAFC"),
        FontSize = 22,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 0, 0, 10)
    };
}

private TextBlock CreateHelpHeading(string text)
{
    return new TextBlock
    {
        Text = text,
        Foreground = GetBrush("PrimaryTextBrush", "#F8FAFC"),
        FontSize = 16,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 18, 0, 8)
    };
}

private TextBlock CreateHelpText(string text)
{
    return new TextBlock
    {
        Text = text,
        Foreground = GetBrush("MutedTextBrush", "#94A3B8"),
        TextWrapping = TextWrapping.Wrap,
        LineHeight = 20,
        Margin = new Thickness(0, 0, 0, 8)
    };
}

private WpfTextBox CreateHelpCode(string text)
{
    return new WpfTextBox
    {
        Text = text,
        IsReadOnly = true,
        Background = GetBrush("InputBrush", "#0B1220"),
        Foreground = GetBrush("PrimaryTextBrush", "#F8FAFC"),
        BorderBrush = GetBrush("InputBorderBrush", "#334155"),
        BorderThickness = new Thickness(1),
        FontFamily = new WpfFontFamily("Consolas"),
        FontSize = 13,
        Padding = new Thickness(12, 10, 12, 10),
        TextWrapping = TextWrapping.NoWrap,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        Margin = new Thickness(0, 0, 0, 8)
    };
}

private WpfBrush GetBrush(string resourceKey, string fallbackColor)
{
    return TryFindResource(resourceKey) as WpfBrush ??
           (WpfBrush)new BrushConverter().ConvertFromString(fallbackColor)!;
}


    private void BrowseAhkExeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executable (*.exe)|*.exe|Alle Dateien (*.*)|*.*",
            CheckFileExists = true,
            Title = "AutoHotkey EXE auswählen"
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.AutoHotkeyPath = dialog.FileName;
        }
    }

    private void BrowseScriptButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "AutoHotkey Script (*.ahk)|*.ahk|Alle Dateien (*.*)|*.*",
            CheckFileExists = true,
            Title = "AutoHotkey Script auswählen"
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.ScriptPath = dialog.FileName;
        }
    }

    private void BrowseChatLogDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "SCUM Chatlog-Ordner auswählen",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            _viewModel.ChatLogDirectory = dialog.SelectedPath;
        }
    }

    private void UpdateTimer()
    {
        _timer.Stop();

        if (!_viewModel.AutoRefreshEnabled)
        {
            return;
        }

        var seconds = Math.Max(1, _viewModel.RefreshIntervalSeconds);
        _timer.Interval = TimeSpan.FromSeconds(seconds);
        _timer.Start();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _remoteSyncTimer.Stop();
        _viewModel.SaveSettings();
    }
}