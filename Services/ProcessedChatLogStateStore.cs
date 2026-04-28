using System.IO;
using System.Text.Json;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class ProcessedChatLogStateStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public ProcessedChatLogState Load()
    {
        if (!File.Exists(AppPaths.ProcessedChatLogStateFile))
        {
            return new ProcessedChatLogState();
        }

        try
        {
            var json = File.ReadAllText(AppPaths.ProcessedChatLogStateFile);
            return JsonSerializer.Deserialize<ProcessedChatLogState>(json) ?? new ProcessedChatLogState();
        }
        catch
        {
            return new ProcessedChatLogState();
        }
    }

    public void Save(ProcessedChatLogState state)
    {
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        File.WriteAllText(AppPaths.ProcessedChatLogStateFile, json);
    }
}