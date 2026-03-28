namespace SimpleConnect.Functions.Models;

public record CreateInviteResponse(
    string InviteUrl,
    string ShortInviteUrl,
    DateTimeOffset ExpiresAt);
