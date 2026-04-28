using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Renci.SshNet;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class SftpLogSyncService : IRemoteLogSyncService
{
    private static readonly Regex ChatLogRegex =
        new(@"^chat_(\d{14})\.log$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<RemoteLogSyncResult> SyncLatestChatLogAsync(RemoteLogSettings settings)
    {
        try
        {
            var port = settings.RemotePort > 0 ? settings.RemotePort : 22;

            using var client = new SftpClient(
                settings.RemoteHost,
                port,
                settings.RemoteUsername,
                settings.RemotePassword);

            client.Connect();

            var entries = client.ListDirectory(settings.RemoteLogsPath)
                .Where(x => !x.IsDirectory && ChatLogRegex.IsMatch(x.Name))
                .Select(x => new
                {
                    x.Name,
                    Match = ChatLogRegex.Match(x.Name)
                })
                .OrderByDescending(x => x.Match.Groups[1].Value)
                .ToList();

            var latest = entries.FirstOrDefault();
            if (latest is null)
            {
                return Task.FromResult(RemoteLogSyncResult.Fail("Keine chat_*.log Datei per SFTP gefunden."));
            }

            var remotePath = $"{settings.RemoteLogsPath.TrimEnd('/')}/{latest.Name}";
            var localPath = Path.Combine(AppPaths.RemoteLogsDirectory, latest.Name);

            using (var fs = File.Create(localPath))
            {
                client.DownloadFile(remotePath, fs);
            }

            client.Disconnect();

            return Task.FromResult(RemoteLogSyncResult.Ok(
                $"SFTP Sync erfolgreich: {latest.Name}",
                localPath,
                latest.Name));
        }
        catch (Exception ex)
        {
            return Task.FromResult(RemoteLogSyncResult.Fail($"SFTP Fehler: {ex.Message}"));
        }
    }
}