using FluentFTP;
using FluentFTP.Helpers;
using ScumFreeBot.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScumFreeBot.Services;

public sealed class FtpLogSyncService : IRemoteLogSyncService
{
    private static readonly Regex ChatLogRegex =
        new(@"^chat_(\d{14})\.log$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<RemoteLogSyncResult> SyncLatestChatLogAsync(RemoteLogSettings settings)
    {
        try
        {
            var port = settings.RemotePort > 0 ? settings.RemotePort : 21;

            using var client = new AsyncFtpClient(
                settings.RemoteHost,
                settings.RemoteUsername,
                settings.RemotePassword,
                port);

            await client.AutoConnect();

            var list = await client.GetListing(settings.RemoteLogsPath);

            var latest = list
                .Where(x => x.Type == FtpObjectType.File && ChatLogRegex.IsMatch(x.Name))
                .Select(x => new
                {
                    x.Name,
                    Match = ChatLogRegex.Match(x.Name)
                })
                .OrderByDescending(x => x.Match.Groups[1].Value)
                .FirstOrDefault();

            if (latest is null)
            {
                await client.Disconnect();
                return RemoteLogSyncResult.Fail("Keine chat_*.log Datei per FTP gefunden.");
            }

            var remotePath = $"{settings.RemoteLogsPath.TrimEnd('/')}/{latest.Name}";
            var localPath = Path.Combine(AppPaths.RemoteLogsDirectory, latest.Name);

            var status = await client.DownloadFile(localPath, remotePath, FtpLocalExists.Overwrite, FtpVerify.None);

            await client.Disconnect();

            if (status.IsSuccess())
            {
                return RemoteLogSyncResult.Ok(
                    $"FTP Sync erfolgreich: {latest.Name}",
                    localPath,
                    latest.Name);
            }

            return RemoteLogSyncResult.Fail($"FTP Download fehlgeschlagen: {status}");
        }
        catch (Exception ex)
        {
            return RemoteLogSyncResult.Fail($"FTP Fehler: {ex.Message}");
        }
    }
}