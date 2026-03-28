namespace SimpleConnect.Functions.Models;

public record InviteSummaryItem(
    string GuestName,
    string ShortInviteUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool Used);

public record RoomSummary(
    string RoomId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int InviteCount,
    List<InviteSummaryItem> Invites);
