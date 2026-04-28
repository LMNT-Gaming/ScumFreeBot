using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScumFreeBot.Services;

public sealed class ChatLogMonitorService
{
    private readonly ChatLogFileResolver _fileResolver = new();
    private readonly ProcessedChatLogStateStore _stateStore = new();

    public List<string> ReadNewLines(string chatLogDirectory)
    {
        var result = new List<string>();

        var latestFile = _fileResolver.GetLatestChatLogFile(chatLogDirectory);
        if (string.IsNullOrWhiteSpace(latestFile) || !File.Exists(latestFile))
        {
            return result;
        }

        var state = _stateStore.Load();

        using var stream = new FileStream(
            latestFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        if (!string.Equals(state.LastFilePath, latestFile, StringComparison.OrdinalIgnoreCase))
        {
            state.LastFilePath = latestFile;
            state.LastBytePosition = 0;
        }

        if (state.LastBytePosition < 0 || state.LastBytePosition > stream.Length)
        {
            state.LastBytePosition = 0;
        }

        stream.Seek(state.LastBytePosition, SeekOrigin.Begin);

        using var reader = new StreamReader(stream, Encoding.Unicode, detectEncodingFromByteOrderMarks: true);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
            {
                result.Add(line);
            }
        }

        state.LastBytePosition = stream.Position;
        _stateStore.Save(state);

        return result;
    }

    public void Reset()
    {
        _stateStore.Save(new Models.ProcessedChatLogState());
    }
}