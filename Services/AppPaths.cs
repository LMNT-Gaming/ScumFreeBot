using System;
using System.IO;

namespace ScumFreeBot.Services;

public static class AppPaths
{
    public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

    public static string DataDirectory
    {
        get
        {
            var path = Path.Combine(BaseDirectory, "Data");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string AppDataDirectory
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ScumFreeBot");

            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string RemoteLogsDirectory
    {
        get
        {
            var path = Path.Combine(AppDataDirectory, "RemoteLogs");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string SettingsFile => Path.Combine(AppDataDirectory, "settings.json");
    public static string PlayerStateFile => Path.Combine(AppDataDirectory, "player-state.json");
    public static string ProcessedChatLogStateFile => Path.Combine(AppDataDirectory, "processed-chatlog-state.json");

    public static string WelcomePackItemsFile => Path.Combine(DataDirectory, "welcomepack-items.json");
    public static string CommandCenterConfigFile => Path.Combine(DataDirectory, "command-center.json");
    public static string CachedRemoteChatLogFile => Path.Combine(RemoteLogsDirectory, "latest-chat.log");
}