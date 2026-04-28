namespace ScumFreeBot.Models;

public sealed class RemoteLogSyncResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string LocalFilePath { get; init; } = string.Empty;
    public string RemoteFileName { get; init; } = string.Empty;

    public static RemoteLogSyncResult Ok(string message, string localFilePath, string remoteFileName) => new()
    {
        Success = true,
        Message = message,
        LocalFilePath = localFilePath,
        RemoteFileName = remoteFileName
    };

    public static RemoteLogSyncResult Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}