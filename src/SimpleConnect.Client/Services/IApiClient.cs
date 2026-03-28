using SimpleConnect.Client.Services;

namespace SimpleConnect.Client.Services;

public record RoomInviteInfo(string GuestName, string InviteUrl, string ShortInviteUrl);
public record RoomInfo(string RoomId, List<RoomInviteInfo> Invites, string AdminAcsToken, string AdminUserId, DateTimeOffset ExpiresAt);
public record InviteInfo(string InviteUrl, string ShortInviteUrl, DateTimeOffset ExpiresAt);
public record JoinInfo(string AcsToken, string AcsUserId, string RoomId, string? DisplayName, string SessionId);
public record InviteSummaryItem(string GuestName, string ShortInviteUrl, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, bool Used);
public record RoomSummary(string RoomId, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, int InviteCount, List<InviteSummaryItem>? Invites);
public record RejoinInfo(string RoomId, string AdminAcsToken, string AdminUserId, DateTimeOffset ExpiresAt);

public interface IApiClient
{
    Task<RoomInfo> CreateRoomAsync(int durationHours, string? guestName1 = null, string? guestName2 = null);
    Task<InviteInfo> CreateInviteAsync(string roomId, int expiryHours, string? guestName = null);
    Task<JoinInfo> JoinRoomAsync(string roomId, string token, string? sessionId = null);
    Task DeleteRoomAsync(string roomId);
    Task<List<RoomSummary>> GetActiveRoomsAsync();
    Task<RejoinInfo> RejoinRoomAsync(string roomId);
}
