using System;
using System.Threading.Tasks;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class RemoteLogCoordinatorService
{
    private readonly FtpLogSyncService _ftpService = new();
    private readonly SftpLogSyncService _sftpService = new();

    public Task<RemoteLogSyncResult> SyncAsync(RemoteLogSettings settings)
    {
        var mode = settings.LogSourceMode?.Trim() ?? "Local";

        return mode.ToUpperInvariant() switch
        {
            "FTP" => _ftpService.SyncLatestChatLogAsync(settings),
            "SFTP" => _sftpService.SyncLatestChatLogAsync(settings),
            _ => Task.FromResult(RemoteLogSyncResult.Fail("Remote Sync ist nur für FTP oder SFTP verfügbar."))
        };
    }
}