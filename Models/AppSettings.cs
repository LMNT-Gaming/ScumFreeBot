namespace ScumFreeBot.Models;

public sealed class AppSettings
{
    public string AutoHotkeyPath { get; set; } = @"C:\Program Files\AutoHotkey\v2\AutoHotkey64.exe";
    public string ScriptPath { get; set; } = @"C:\ScumFreeBot\scum_action.ahk";
    public string ChatLogDirectory { get; set; } = string.Empty;

    public bool AutoRefreshEnabled { get; set; } = true;
    public int RefreshIntervalSeconds { get; set; } = 2;
    public string TestCommand { get; set; } = "#ListPlayers";

    public string LogSourceMode { get; set; } = "Local"; // Local, FTP, SFTP
    public bool RemoteSyncEnabled { get; set; } = false;
    public string RemoteHost { get; set; } = string.Empty;
    public int RemotePort { get; set; } = 22;
    public string RemoteUsername { get; set; } = string.Empty;
    public string RemotePassword { get; set; } = string.Empty;
    public string RemoteLogsPath { get; set; } = "/SCUM/Saved/SaveFiles/Logs";
    public int RemoteSyncIntervalSeconds { get; set; } = 5;
}