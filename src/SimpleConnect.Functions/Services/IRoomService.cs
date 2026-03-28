namespace SimpleConnect.Functions.Services;

public record CreateRoomResult(
    string RoomId,
    string AdminAcsToken,
    string AdminUserId,
    DateTimeOffset ExpiresAt);

public record JoinRoomResult(
    string AcsToken,
    string AcsUserId);

public interface IRoomService
{
    Task<CreateRoomResult> CreateRoomAsync(int? roomDurationHours = null);
    Task<JoinRoomResult> AddGuestToRoomAsync(string roomId);
    Task<CreateRoomResult> RejoinRoomAsAdminAsync(string roomId);
    Task DeleteRoomAsync(string roomId);
}
