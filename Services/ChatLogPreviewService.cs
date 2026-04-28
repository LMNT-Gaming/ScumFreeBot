using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ScumFreeBot.Services;

public sealed class ChatLogPreviewService
{
    public string ReadLastLines(string filePath, int lineCount = 25)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return "Keine Chatlog-Datei gefunden.";
        }

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            using var reader = new StreamReader(stream, Encoding.Unicode, detectEncodingFromByteOrderMarks: true);

            var content = reader.ReadToEnd();

            var lines = content
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .TakeLast(Math.Max(1, lineCount))
                .ToArray();

            if (lines.Length == 0)
            {
                return "Die Chatlog-Datei ist leer.";
            }

            return string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            return $"Fehler beim Lesen des Chatlogs: {ex.Message}";
        }
    }
}