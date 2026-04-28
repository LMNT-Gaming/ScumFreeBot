using System;
using System.Diagnostics;
using System.Security.Principal;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

namespace ScumFreeBot.Services;

public static class AdminPrivilegeService
{
    public static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void RestartAsAdministrator()
    {
        var exePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(exePath))
        {
            WpfMessageBox.Show(
    "Der Programm-Pfad konnte nicht ermittelt werden.",
    "Administratorprüfung",
    WpfMessageBoxButton.OK,
    WpfMessageBoxImage.Warning);

            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            Process.Start(startInfo);
            WpfApplication.Current.Shutdown();
        }
        catch
        {
            // User hat UAC abgebrochen.
        }
    }
}