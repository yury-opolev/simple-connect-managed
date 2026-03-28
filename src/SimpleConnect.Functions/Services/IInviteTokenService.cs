namespace SimpleConnect.Functions.Services;

public record InviteTokenClaims(string RoomId, string TokenId, string? GuestName);
public record GeneratedToken(string Token, string TokenId);

public interface IInviteTokenService
{
    GeneratedToken GenerateInviteToken(string roomId, int? expiryHours = null, string? guestName = null);
    InviteTokenClaims? ValidateInviteToken(string token, string expectedRoomId);
}
