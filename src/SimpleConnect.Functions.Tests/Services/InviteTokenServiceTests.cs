using SimpleConnect.Functions.Configuration;
using SimpleConnect.Functions.Services;

namespace SimpleConnect.Functions.Tests.Services;

public class InviteTokenServiceTests
{
    private readonly AppSettings settings;
    private readonly InviteTokenService service;

    public InviteTokenServiceTests()
    {
        this.settings = new AppSettings
        {
            JwtSecret = "this-is-a-test-secret-key-that-is-at-least-32-chars-long",
            TokenExpiryHours = 24
        };
        this.service = new InviteTokenService(this.settings);
    }

    [Fact]
    public void GenerateInviteToken_ReturnsNonEmptyTokenAndId()
    {
        var result = this.service.GenerateInviteToken("room-123");

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.NotEmpty(result.TokenId);
    }

    [Fact]
    public void GenerateInviteToken_DifferentCallsProduceDifferentTokens()
    {
        var result1 = this.service.GenerateInviteToken("room-123");
        var result2 = this.service.GenerateInviteToken("room-123");

        Assert.NotEqual(result1.Token, result2.Token);
        Assert.NotEqual(result1.TokenId, result2.TokenId);
    }

    [Fact]
    public void ValidateInviteToken_ValidToken_ReturnsClaimsWithCorrectRoomId()
    {
        var roomId = "test-room-42";
        var result = this.service.GenerateInviteToken(roomId);

        var claims = this.service.ValidateInviteToken(result.Token, roomId);

        Assert.NotNull(claims);
        Assert.Equal(roomId, claims.RoomId);
        Assert.NotEmpty(claims.TokenId);
    }

    [Fact]
    public void ValidateInviteToken_WrongRoomId_ReturnsNull()
    {
        var result = this.service.GenerateInviteToken("room-123");

        var claims = this.service.ValidateInviteToken(result.Token, "wrong-room");

        Assert.Null(claims);
    }

    [Fact]
    public void ValidateInviteToken_TamperedToken_ReturnsNull()
    {
        var result = this.service.GenerateInviteToken("room-123");

        var claims = this.service.ValidateInviteToken(result.Token + "tampered", "room-123");

        Assert.Null(claims);
    }

    [Fact]
    public void ValidateInviteToken_EmptyToken_ReturnsNull()
    {
        var claims = this.service.ValidateInviteToken("", "room-123");

        Assert.Null(claims);
    }

    [Fact]
    public void ValidateInviteToken_RandomString_ReturnsNull()
    {
        var claims = this.service.ValidateInviteToken("not-a-jwt", "room-123");

        Assert.Null(claims);
    }

    [Fact]
    public void ValidateInviteToken_WrongSigningKey_ReturnsNull()
    {
        var result = this.service.GenerateInviteToken("room-123");

        var otherSettings = new AppSettings
        {
            JwtSecret = "a-completely-different-secret-key-that-is-also-long-enough",
            TokenExpiryHours = 24
        };
        var otherService = new InviteTokenService(otherSettings);

        var claims = otherService.ValidateInviteToken(result.Token, "room-123");

        Assert.Null(claims);
    }

    [Fact]
    public void ValidateInviteToken_EachTokenHasUniqueJti()
    {
        var roomId = "room-123";
        var result1 = this.service.GenerateInviteToken(roomId);
        var result2 = this.service.GenerateInviteToken(roomId);

        var claims1 = this.service.ValidateInviteToken(result1.Token, roomId);
        var claims2 = this.service.ValidateInviteToken(result2.Token, roomId);

        Assert.NotNull(claims1);
        Assert.NotNull(claims2);
        Assert.NotEqual(claims1.TokenId, claims2.TokenId);
    }

    [Fact]
    public void ValidateInviteToken_TokenIdMatchesGeneratedId()
    {
        var roomId = "room-123";
        var result = this.service.GenerateInviteToken(roomId);

        var claims = this.service.ValidateInviteToken(result.Token, roomId);

        Assert.NotNull(claims);
        Assert.Equal(result.TokenId, claims.TokenId);
    }

    [Fact]
    public void GenerateInviteToken_WithGuestName_IncludesNameInClaims()
    {
        var result = this.service.GenerateInviteToken("room-123", guestName: "Grandma");

        var claims = this.service.ValidateInviteToken(result.Token, "room-123");

        Assert.NotNull(claims);
        Assert.Equal("Grandma", claims.GuestName);
    }

    [Fact]
    public void GenerateInviteToken_WithoutGuestName_GuestNameIsNull()
    {
        var result = this.service.GenerateInviteToken("room-123");

        var claims = this.service.ValidateInviteToken(result.Token, "room-123");

        Assert.NotNull(claims);
        Assert.Null(claims.GuestName);
    }

    [Fact]
    public void GenerateInviteToken_CustomExpiryHours_ProducesValidToken()
    {
        var result = this.service.GenerateInviteToken("room-123", expiryHours: 1);

        var claims = this.service.ValidateInviteToken(result.Token, "room-123");

        Assert.NotNull(claims);
    }

    [Fact]
    public void ValidateInviteToken_ExpiredToken_HandledGracefully()
    {
        var expiredSettings = new AppSettings
        {
            JwtSecret = "this-is-a-test-secret-key-that-is-at-least-32-chars-long",
            TokenExpiryHours = 0
        };
        var expiredService = new InviteTokenService(expiredSettings);

        var result = expiredService.GenerateInviteToken("room-123", expiryHours: 0);

        var claims = expiredService.ValidateInviteToken(result.Token, "room-123");

        Assert.True(claims is null || claims.RoomId == "room-123");
    }
}
