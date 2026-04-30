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
using System.Diagnostics;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

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
        await _viewModel.CheckForUpdatesAsync();
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

    private void RefreshPlayerCommandUsagesButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.LoadPlayerCommandUsages();
    }

    private void ResetSelectedPlayerCommandUsageButton_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            "Diesen Befehl für den ausgewählten Spieler wirklich zurücksetzen?",
            "Befehl zurücksetzen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _viewModel.ResetSelectedPlayerCommandUsage();
    }

    private void ResetAllPlayerCommandUsagesButton_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            "Alle gespeicherten Befehlsausführungen für diesen Spieler wirklich zurücksetzen?",
            "Spieler zurücksetzen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _viewModel.ResetAllCommandUsagesForSelectedPlayer();
    }

    private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.CheckForUpdatesAsync();
    }

    private void OpenUpdateDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl(_viewModel.UpdateDownloadUrl);
    }

    private void OpenPatchNotesButton_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl(_viewModel.UpdatePatchNotesUrl);
    }

    private void OpenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        catch
        {
            System.Windows.MessageBox.Show(
                this,
                "Der Link konnte nicht geöffnet werden.",
                "Update",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
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
        Background = GetBrush("WindowBackgroundBrush", "#07070A"),
        ShowInTaskbar = false
    };



    var panel = new StackPanel
    {
        Margin = new Thickness(22)
    };

    panel.Children.Add(CreateHelpTitle("Script-Hilfe fuer Befehle"));
        panel.Children.Add(CreateHelpTitle("Script-Hilfe fuer Befehle"));
        panel.Children.Add(CreateHelpText(
            "Hier findest du die wichtigsten Variablen, Ablaufbefehle und Beispiele fuer .sfb-Skripte. " +
            "Die Skripte liegen im Data-Ordner und werden von der Steuerungszentrale ausgefuehrt."));

        panel.Children.Add(CreateHelpHeading("1. Wichtige Variablen"));
        panel.Children.Add(CreateHelpCode(
        @"{player}              Spielername
{steamId}             Steam-ID des Spielers
{command}             Ausgeloester Spielerbefehl, z. B. !vote
{args}                Alle Argumente nach dem Spielerbefehl
{arg1}                Erstes Argument
{arg2}                Zweites Argument
{arg3}                Drittes Argument
{now}                 Aktuelles Datum mit Uhrzeit
{date}                Aktuelles Datum
{time}                Aktuelle Uhrzeit"));

        panel.Children.Add(CreateHelpHeading("2. Spielerposition"));
        panel.Children.Add(CreateHelpText(
            "Die Spielerposition wird nur abgefragt, wenn das Skript wirklich {playerlocation} verwendet. " +
            "Der Bot fuehrt dann automatisch #ListPlayers aus und liest die Position aus der lokalen SCUM.log."));

        panel.Children.Add(CreateHelpCode(
        @"{playerlocation}        Spielerposition im SCUM-Format
{playerlocation+50}     Spielerposition mit Z-Offset +50
{playerlocation+5}      Spielerposition mit Z-Offset +5
{playerlocation-10}     Spielerposition mit Z-Offset -10"));

        panel.Children.Add(CreateHelpText(
            "Ausgabeformat fuer SCUM: \"[X Y Z]\". Beispiel: \"[-554320 -846077.312 13288.3]\""));

        panel.Children.Add(CreateHelpHeading("3. Warten zwischen Befehlen"));
        panel.Children.Add(CreateHelpCode(
        @"wait 500ms
wait 1s
wait 30s
wait 2m
wait 1h"));

        panel.Children.Add(CreateHelpText(
            "wait pausiert den Ablauf genau an dieser Stelle. So kann der Admin selbst bestimmen, " +
            "wie viel Abstand zwischen Teleport, Spawn und Chatmeldung liegt."));

        panel.Children.Add(CreateHelpHeading("4. Kommentare"));
        panel.Children.Add(CreateHelpCode(
        @"// Kommentar
# Kommentar
; Kommentar"));

        panel.Children.Add(CreateHelpText(
            "Wichtig: SCUM-Adminbefehle wie #spawnitem bleiben gueltig. " +
            "Nur Zeilen mit '# ' also Raute plus Leerzeichen werden als Kommentar ignoriert."));

        panel.Children.Add(CreateHelpHeading("5. Randomizer / Zufallsbloecke"));
        panel.Children.Add(CreateHelpText(
            "Mit randomblock kann pro Ausfuehrung zufaellig ein case-Block ausgefuehrt werden. " +
            "Die Zahl hinter case ist eine Gewichtung. Die Summe muss nicht 100 ergeben."));

        panel.Children.Add(CreateHelpCode(
        @"randomblock
case 70
  #spawnitem BP_Water_05L 1 Location {playerlocation+50}
  wait 500ms
  #spawnitem BP_Bread 1 Location {playerlocation+50}

case 20
  #spawnitem Antibiotic_Pill_Single 2 Location {playerlocation+50}
  wait 500ms
  #spawnitem Painkillers_01 1 Location {playerlocation+50}

case 10
  #spawnitem BP_Cash 1000 Location {playerlocation+50}

endrandomblock"));

        panel.Children.Add(CreateHelpText(
            "Beispiel: case 70 / case 20 / case 10 entspricht ungefaehr 70% / 20% / 10%. " +
            "case 20 / case 60 entspricht automatisch 25% / 75%. Ohne Zahl zaehlt case als Gewicht 1."));


        panel.Children.Add(CreateHelpHeading("5. Randomizer / Zufallsbloecke"));
        panel.Children.Add(CreateHelpText(
            "Mit randomblock kann pro Ausfuehrung zufaellig ein case-Block ausgefuehrt werden."));

        panel.Children.Add(CreateHelpCode(
        @"randomblock
case 70
  #spawnitem BP_Water_05L 1 Location {playerlocation+50}
  wait 500ms
  #spawnitem BP_Bread 1 Location {playerlocation+50}

case 20
  #spawnitem Antibiotic_Pill_Single 2 Location {playerlocation+50}
  wait 500ms
  #spawnitem Painkillers_01 1 Location {playerlocation+50}

case 10
  #spawnitem BP_Cash 1000 Location {playerlocation+50}

endrandomblock"));

        panel.Children.Add(CreateHelpText(
            "Ein randomblock beginnt immer mit randomblock und endet mit endrandomblock. " +
            "Dazwischen koennen mehrere case-Bloecke stehen. Pro Ausfuehrung wird genau ein case-Block zufaellig ausgewaehlt."));

        panel.Children.Add(CreateHelpText(
            "Die Zahl hinter case ist eine Gewichtung, keine feste Prozentpflicht. " +
            "Beispiel: case 70, case 20, case 10 ergibt ungefaehr 70%, 20%, 10%. " +
            "case 20 und case 60 ergibt automatisch 25% und 75%, weil 20 von 80 und 60 von 80 gerechnet wird."));

        panel.Children.Add(CreateHelpText(
            "Wenn bei case keine Zahl angegeben wird, bekommt dieser Block automatisch Gewicht 1. " +
            "Mehrere case ohne Zahl werden gleich wahrscheinlich ausgewaehlt."));

        panel.Children.Add(CreateHelpHeading("6. Beispiel: Vote-Befehl"));
        panel.Children.Add(CreateHelpCode(
        @"Spieler schreibt:
!vote settime 1

Skript:
#vote {arg1} {arg2}

Ergebnis:
#vote settime 1"));

        panel.Children.Add(CreateHelpHeading("7. Beispiel mit allen Argumenten"));
        panel.Children.Add(CreateHelpCode(
        @"Spieler schreibt:
!vote setweather clear

Skript:
#vote {args}

Ergebnis:
#vote setweather clear"));

        panel.Children.Add(CreateHelpHeading("8. Welcomepack-Beispiel"));
        panel.Children.Add(CreateHelpCode(
        @"{player} dein Welcomepack ist auf dem Weg, bitte bleib stehen!
wait 1s

#teleportto {player}
wait 30s

#spawnitem BP_Weapon_98k_Kar98 1 Location {playerlocation+50}
wait 500ms

#spawnitem BP_Weapon_Magazine_Kar98 2 Location {playerlocation+50}
wait 500ms

{player} dein Welcomepack wurde zugestellt."));

        panel.Children.Add(CreateHelpHeading("9. KI-Hilfe"));
        panel.Children.Add(CreateHelpText(
            "Mit dem Button kannst du eine Vorlage kopieren und in eine KI einfuegen. " +
            "Danach musst du nur noch beschreiben, welche Items, Waffen oder Ablaufe du haben moechtest."));

        panel.Children.Add(CreateCopyAiPromptButton());

        var closeButton = new WpfButton
    {
        Content = "Schliessen",
        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
        Margin = new Thickness(0, 18, 0, 0),
        Padding = new Thickness(16, 8, 16, 8),
        Background = GetBrush("AccentBrush", "#EF1B1B"),
        Foreground = WpfBrushes.White,
        BorderBrush = GetBrush("AccentBrush", "#EF1B1B"),
        BorderThickness = new Thickness(1)
    };
    closeButton.Click += (_, _) => dialog.Close();
    panel.Children.Add(closeButton);

    dialog.Content = new Border
    {
        Background = GetBrush("CardBackgroundBrush", "#101014"),
        BorderBrush = GetBrush("CardBorderBrush", "#2A1116"),
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

    private FrameworkElement CreateCopyAiPromptButton()
    {
        var button = new WpfButton
        {
            Content = "KI-Prompt kopieren",
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 14),
            Padding = new Thickness(14, 8, 14, 8),
            Foreground = WpfBrushes.White,
            Background = GetBrush("AccentBrush", "#EF1B1B"),
            BorderBrush = GetBrush("AccentBrush", "#EF1B1B"),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        button.Click += (_, _) =>
        {
            WpfClipboard.SetText(BuildAiScriptPrompt());

            WpfMessageBox.Show(
                this,
                "KI-Prompt wurde in die Zwischenablage kopiert.",
                "Kopiert",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Information);
        };

        return button;
    }

    private static string BuildAiScriptPrompt()
    {
        return
    @"Erstelle mir ein SCUM Freebot .sfb Skript.

Bitte nutze nur diese unterstuetzte Syntax:

Variablen:
{player}              Spielername
{steamId}             Steam-ID des Spielers
{command}             Ausgeloester Spielerbefehl
{args}                Alle Argumente nach dem Spielerbefehl
{arg1}                Erstes Argument
{arg2}                Zweites Argument
{arg3}                Drittes Argument
{now}                 Aktuelles Datum mit Uhrzeit
{date}                Aktuelles Datum
{time}                Aktuelle Uhrzeit

Spielerposition:
{playerlocation}      Position im SCUM-Format ""[X Y Z]""
{playerlocation+50}   Position mit Z-Offset +50
{playerlocation+5}    Position mit Z-Offset +5
{playerlocation-10}   Position mit Z-Offset -10

Warten:
wait 500ms
wait 1s
wait 30s
wait 2m
wait 1h

Kommentare:
Zeilen mit // oder ; sind Kommentare.
Zeilen mit '# ' sind Kommentare.
SCUM-Adminbefehle wie #spawnitem bleiben gueltig.

Randomizer:
randomblock
case 70
  Befehl A1
  wait 500ms
  Befehl A2

case 20
  Befehl B1

case 10
  Befehl C1

endrandomblock

Die case-Zahlen sind Gewichtungen. Sie muessen nicht zusammen 100 ergeben.

Wichtig:
- Gib nur den fertigen .sfb Skriptinhalt aus.
- Keine Markdown-Codeblöcke.
- Keine Erklaerung.
- Nutze fuer Spawnpositionen bevorzugt Location {playerlocation+50}, damit Items nicht im Boden stecken.
- Zwischen mehreren Spawnbefehlen bitte wait 500ms einfuegen.

Mein Wunsch:
Erstelle mir ein Skript fuer: ";
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
        Background = GetBrush("InputBrush", "#09090B"),
        Foreground = GetBrush("PrimaryTextBrush", "#F8FAFC"),
        BorderBrush = GetBrush("InputBorderBrush", "#3F1D24"),
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