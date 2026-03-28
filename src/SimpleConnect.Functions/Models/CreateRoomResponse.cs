namespace SimpleConnect.Functions.Models;

public record CreateRoomInvite(string GuestName, string InviteUrl, string ShortInviteUrl);

public record CreateRoomResponse(
    string RoomId,
    List<CreateRoomInvite> Invites,
    string AdminAcsToken,
    string AdminUserId,
    DateTimeOffset ExpiresAt);
