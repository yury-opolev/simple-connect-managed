using SimpleConnect.Functions.Models;

namespace SimpleConnect.Functions.Services;

public interface IRoomStoreService
{
    Task SaveRoomAsync(string roomId, DateTimeOffset expiresAt);
    Task<List<RoomEntity>> GetActiveRoomsAsync();
    Task DeleteRoomRecordAsync(string roomId);
    Task IncrementInviteCountAsync(string roomId);
    Task<string> SaveInviteAsync(string roomId, string tokenId, string guestName, string inviteUrl, DateTimeOffset expiresAt);
    Task<List<InviteEntity>> GetInvitesForRoomAsync(string roomId);
    Task<string?> GetShortLinkUrlAsync(string code);
}
