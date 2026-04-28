using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScumFreeBot.Services;

public sealed class ChatLogFileResolver
{
    private static readonly Regex ChatLogRegex =
        new(@"^chat_(\d{14})\.log$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string? GetLatestChatLogFile(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return null;
        }

        var files = Directory
            .GetFiles(directoryPath, "chat_*.log", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .Select(file => new
            {
                File = file,
                Match = ChatLogRegex.Match(file.Name)
            })
            .Where(x => x.Match.Success)
            .Select(x => new
            {
                x.File.FullName,
                Timestamp = x.Match.Groups[1].Value
            })
            .OrderByDescending(x => x.Timestamp)
            .ToList();

        return files.FirstOrDefault()?.FullName;
    }
}