using SimpleConnect.Functions.Functions;
using SimpleConnect.Functions.Models;

namespace SimpleConnect.Functions.Tests.Models;

public class ResponseModelsTests
{
    [Fact]
    public void CreateRoomResponse_StoresAllProperties()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var invites = new List<CreateRoomInvite>
        {
            new("Grandma", "https://example.com/join?token=abc", "https://example.com/s/abc123"),
            new("Grandpa", "https://example.com/join?token=def", "https://example.com/s/def456")
        };
        var response = new CreateRoomResponse(
            RoomId: "room-1",
            Invites: invites,
            AdminAcsToken: "acs-token",
            AdminUserId: "user-1",
            ExpiresAt: expiresAt);

        Assert.Equal("room-1", response.RoomId);
        Assert.Equal(2, response.Invites.Count);
        Assert.Equal("Grandma", response.Invites[0].GuestName);
        Assert.Equal("Grandpa", response.Invites[1].GuestName);
        Assert.Equal("https://example.com/s/abc123", response.Invites[0].ShortInviteUrl);
        Assert.Equal("https://example.com/s/def456", response.Invites[1].ShortInviteUrl);
        Assert.Equal("acs-token", response.AdminAcsToken);
        Assert.Equal("user-1", response.AdminUserId);
        Assert.Equal(expiresAt, response.ExpiresAt);
    }

    [Fact]
    public void CreateRoomResponse_WithEmptyInvites_HasZeroCount()
    {
        var response = new CreateRoomResponse(
            RoomId: "room-1",
            Invites: new List<CreateRoomInvite>(),
            AdminAcsToken: "token",
            AdminUserId: "user",
            ExpiresAt: DateTimeOffset.UtcNow);

        Assert.Empty(response.Invites);
    }

    [Fact]
    public void CreateRoomInvite_StoresAllProperties()
    {
        var invite = new CreateRoomInvite(
            GuestName: "Grandma",
            InviteUrl: "https://example.com/join?token=abc",
            ShortInviteUrl: "https://example.com/s/abc123");

        Assert.Equal("Grandma", invite.GuestName);
        Assert.Equal("https://example.com/join?token=abc", invite.InviteUrl);
        Assert.Equal("https://example.com/s/abc123", invite.ShortInviteUrl);
    }

    [Fact]
    public void CreateRoomInvite_RecordEquality_Works()
    {
        var i1 = new CreateRoomInvite("Guest", "url", "short");
        var i2 = new CreateRoomInvite("Guest", "url", "short");

        Assert.Equal(i1, i2);
    }

    [Fact]
    public void CreateRoomInvite_RecordInequality_DifferentGuestName()
    {
        var i1 = new CreateRoomInvite("Alice", "url", "short");
        var i2 = new CreateRoomInvite("Bob", "url", "short");

        Assert.NotEqual(i1, i2);
    }

    [Fact]
    public void CreateInviteResponse_StoresAllProperties()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(12);
        var response = new CreateInviteResponse(
            InviteUrl: "https://example.com/join?token=xyz",
            ShortInviteUrl: "https://example.com/s/abc123",
            ExpiresAt: expiresAt);

        Assert.Equal("https://example.com/join?token=xyz", response.InviteUrl);
        Assert.Equal("https://example.com/s/abc123", response.ShortInviteUrl);
        Assert.Equal(expiresAt, response.ExpiresAt);
    }

    [Fact]
    public void JoinRoomResponse_StoresAllProperties()
    {
        var response = new JoinRoomResponse(
            AcsToken: "guest-token",
            AcsUserId: "guest-user",
            RoomId: "room-42",
            DisplayName: "Grandma",
            SessionId: "session-1");

        Assert.Equal("guest-token", response.AcsToken);
        Assert.Equal("guest-user", response.AcsUserId);
        Assert.Equal("room-42", response.RoomId);
        Assert.Equal("Grandma", response.DisplayName);
        Assert.Equal("session-1", response.SessionId);
    }

    [Fact]
    public void CreateRoomResponse_RecordEquality_Works()
    {
        var expiresAt = DateTimeOffset.UtcNow;
        var invites = new List<CreateRoomInvite> { new("Guest", "url", "short") };
        var r1 = new CreateRoomResponse("r1", invites, "token", "user", expiresAt);
        var r2 = new CreateRoomResponse("r1", invites, "token", "user", expiresAt);

        Assert.Equal(r1, r2);
    }

    [Fact]
    public void JoinRoomResponse_RecordInequality_Works()
    {
        var r1 = new JoinRoomResponse("token1", "user1", "room1", "Alice", "s1");
        var r2 = new JoinRoomResponse("token2", "user2", "room1", "Bob", "s2");

        Assert.NotEqual(r1, r2);
    }

    [Fact]
    public void CreateRoomRequest_DefaultValues_AreNull()
    {
        var request = new CreateRoomRequest();

        Assert.Null(request.DurationHours);
        Assert.Null(request.GuestName1);
        Assert.Null(request.GuestName2);
    }

    [Fact]
    public void CreateRoomRequest_StoresAllProperties()
    {
        var request = new CreateRoomRequest(
            DurationHours: 48,
            GuestName1: "Grandma",
            GuestName2: "Grandpa");

        Assert.Equal(48, request.DurationHours);
        Assert.Equal("Grandma", request.GuestName1);
        Assert.Equal("Grandpa", request.GuestName2);
    }

    [Fact]
    public void InviteSummaryItem_StoresAllProperties()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var item = new InviteSummaryItem(
            GuestName: "Grandma",
            ShortInviteUrl: "https://example.com/s/abc123",
            CreatedAt: createdAt,
            ExpiresAt: expiresAt,
            Used: false);

        Assert.Equal("Grandma", item.GuestName);
        Assert.Equal("https://example.com/s/abc123", item.ShortInviteUrl);
        Assert.Equal(createdAt, item.CreatedAt);
        Assert.Equal(expiresAt, item.ExpiresAt);
        Assert.False(item.Used);
    }

    [Fact]
    public void InviteSummaryItem_Used_ReflectsState()
    {
        var item = new InviteSummaryItem("Guest", "url", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), Used: true);

        Assert.True(item.Used);
    }

    [Fact]
    public void RoomSummary_StoresAllProperties()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var invites = new List<InviteSummaryItem>
        {
            new("Grandma", "https://example.com/s/abc", createdAt, expiresAt, false),
            new("Grandpa", "https://example.com/s/def", createdAt, expiresAt, true)
        };

        var summary = new RoomSummary(
            RoomId: "room-1",
            CreatedAt: createdAt,
            ExpiresAt: expiresAt,
            InviteCount: 2,
            Invites: invites);

        Assert.Equal("room-1", summary.RoomId);
        Assert.Equal(createdAt, summary.CreatedAt);
        Assert.Equal(expiresAt, summary.ExpiresAt);
        Assert.Equal(2, summary.InviteCount);
        Assert.Equal(2, summary.Invites.Count);
        Assert.Equal("Grandma", summary.Invites[0].GuestName);
        Assert.False(summary.Invites[0].Used);
        Assert.Equal("Grandpa", summary.Invites[1].GuestName);
        Assert.True(summary.Invites[1].Used);
    }

    [Fact]
    public void RoomSummary_WithEmptyInvites_HasZeroCount()
    {
        var summary = new RoomSummary(
            RoomId: "room-1",
            CreatedAt: DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24),
            InviteCount: 0,
            Invites: new List<InviteSummaryItem>());

        Assert.Empty(summary.Invites);
        Assert.Equal(0, summary.InviteCount);
    }

    [Fact]
    public void RejoinRoomResponse_StoresAllProperties()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(12);
        var response = new RejoinRoomResponse(
            RoomId: "room-1",
            AdminAcsToken: "acs-token",
            AdminUserId: "user-1",
            ExpiresAt: expiresAt);

        Assert.Equal("room-1", response.RoomId);
        Assert.Equal("acs-token", response.AdminAcsToken);
        Assert.Equal("user-1", response.AdminUserId);
        Assert.Equal(expiresAt, response.ExpiresAt);
    }
}
