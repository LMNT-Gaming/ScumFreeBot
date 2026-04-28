namespace ScumFreeBot.Models;

public sealed class RemoteLogSettings
{
    public string LogSourceMode { get; set; } = "Local";
    public string RemoteHost { get; set; } = string.Empty;
    public int RemotePort { get; set; } = 22;
    public string RemoteUsername { get; set; } = string.Empty;
    public string RemotePassword { get; set; } = string.Empty;
    public string RemoteLogsPath { get; set; } = "/SCUM/Saved/SaveFiles/Logs";
}