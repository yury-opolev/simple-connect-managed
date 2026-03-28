namespace SimpleConnect.Functions.Models;

public record JoinRoomResponse(
    string AcsToken,
    string AcsUserId,
    string RoomId,
    string? DisplayName,
    string SessionId);
