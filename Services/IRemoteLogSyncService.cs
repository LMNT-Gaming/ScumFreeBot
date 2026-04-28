using System.Threading.Tasks;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public interface IRemoteLogSyncService
{
    Task<RemoteLogSyncResult> SyncLatestChatLogAsync(RemoteLogSettings settings);
}