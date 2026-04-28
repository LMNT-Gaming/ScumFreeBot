namespace ScumFreeBot.Models;

public sealed class StatusSnapshot
{
    public bool IsScumRunning { get; set; }
    public int ScumProcessCount { get; set; }

    public string AutoHotkeyPath { get; set; } = string.Empty;
    public string ScriptPath { get; set; } = string.Empty;
    public bool IsAutoHotkeyFound { get; set; }
    public bool IsScriptFound { get; set; }

    public string? LatestChatLogFile { get; set; }
    public bool IsChatLogAvailable { get; set; }
    public string ChatLogPreview { get; set; } = "Noch kein Chatlog geladen.";
}