using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class StatusMonitorService
{
    private readonly ChatLogFileResolver _chatLogFileResolver = new();
    private readonly ChatLogPreviewService _chatLogPreviewService = new();

    public Task<StatusSnapshot> CheckAsync(string autoHotkeyPath, string scriptPath, string chatLogDirectory)
    {
        var scumProcesses = Process.GetProcessesByName("SCUM");
        var latestChatLog = _chatLogFileResolver.GetLatestChatLogFile(chatLogDirectory);

        var snapshot = new StatusSnapshot
        {
            IsScumRunning = scumProcesses.Any(),
            ScumProcessCount = scumProcesses.Length,

            AutoHotkeyPath = autoHotkeyPath,
            ScriptPath = scriptPath,
            IsAutoHotkeyFound = !string.IsNullOrWhiteSpace(autoHotkeyPath) && File.Exists(autoHotkeyPath),
            IsScriptFound = !string.IsNullOrWhiteSpace(scriptPath) && File.Exists(scriptPath),

            LatestChatLogFile = latestChatLog,
            IsChatLogAvailable = !string.IsNullOrWhiteSpace(latestChatLog),
            ChatLogPreview = !string.IsNullOrWhiteSpace(latestChatLog)
                ? _chatLogPreviewService.ReadLastLines(latestChatLog, 25)
                : "Kein passendes Chatlog im ausgewählten Ordner gefunden."
        };

        return Task.FromResult(snapshot);
    }
}