namespace SimpleConnect.Functions.Models;

public record RejoinRoomResponse(
    string RoomId,
    string AdminAcsToken,
    string AdminUserId,
    DateTimeOffset ExpiresAt);
