using Azure;
using Azure.Data.Tables;
using Moq;
using SimpleConnect.Functions.Models;
using SimpleConnect.Functions.Services;

namespace SimpleConnect.Functions.Tests.Services;

public class TokenStoreServiceTests
{
    [Fact]
    public async Task TryUseTokenAsync_FirstUse_ReturnsAllowedWithNewSessionId()
    {
        var mockTableClient = new Mock<TableClient>();
        mockTableClient
            .Setup(x => x.AddEntityAsync(
                It.IsAny<UsedTokenEntity>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        var service = new TestableTokenStoreService(mockTableClient.Object);

        var result = await service.TryUseTokenAsync("room-1", "token-1", null);

        Assert.True(result.Allowed);
        Assert.NotEmpty(result.SessionId);
    }

    [Fact]
    public async Task TryUseTokenAsync_SecondUse_NoSessionId_ReturnsFalse()
    {
        var mockTableClient = new Mock<TableClient>();
        mockTableClient
            .Setup(x => x.AddEntityAsync(
                It.IsAny<UsedTokenEntity>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(409, "Entity already exists"));

        var service = new TestableTokenStoreService(mockTableClient.Object);

        var result = await service.TryUseTokenAsync("room-1", "token-1", null);

        Assert.False(result.Allowed);
    }

    [Fact]
    public async Task TryUseTokenAsync_SameSession_ReturnsAllowed()
    {
        var existingEntity = new UsedTokenEntity
        {
            PartitionKey = "room-1",
            RowKey = "token-1",
            SessionId = "session-abc"
        };

        var mockTableClient = new Mock<TableClient>();
        mockTableClient
            .Setup(x => x.AddEntityAsync(
                It.IsAny<UsedTokenEntity>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(409, "Entity already exists"));
        mockTableClient
            .Setup(x => x.GetEntityAsync<UsedTokenEntity>("room-1", "token-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(existingEntity, Mock.Of<Response>()));

        var service = new TestableTokenStoreService(mockTableClient.Object);

        var result = await service.TryUseTokenAsync("room-1", "token-1", "session-abc");

        Assert.True(result.Allowed);
        Assert.Equal("session-abc", result.SessionId);
    }

    [Fact]
    public async Task TryUseTokenAsync_DifferentSession_ReturnsFalse()
    {
        var existingEntity = new UsedTokenEntity
        {
            PartitionKey = "room-1",
            RowKey = "token-1",
            SessionId = "session-abc"
        };

        var mockTableClient = new Mock<TableClient>();
        mockTableClient
            .Setup(x => x.AddEntityAsync(
                It.IsAny<UsedTokenEntity>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(409, "Entity already exists"));
        mockTableClient
            .Setup(x => x.GetEntityAsync<UsedTokenEntity>("room-1", "token-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(existingEntity, Mock.Of<Response>()));

        var service = new TestableTokenStoreService(mockTableClient.Object);

        var result = await service.TryUseTokenAsync("room-1", "token-1", "different-session");

        Assert.False(result.Allowed);
    }

    [Fact]
    public async Task TryUseTokenAsync_StoresCorrectPartitionAndRowKey()
    {
        var mockTableClient = new Mock<TableClient>();
        UsedTokenEntity? capturedEntity = null;
        mockTableClient
            .Setup(x => x.AddEntityAsync(
                It.IsAny<UsedTokenEntity>(),
                It.IsAny<CancellationToken>()))
            .Callback<UsedTokenEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync(Mock.Of<Response>());

        var service = new TestableTokenStoreService(mockTableClient.Object);

        await service.TryUseTokenAsync("my-room", "my-token", null);

        Assert.NotNull(capturedEntity);
        Assert.Equal("my-room", capturedEntity.PartitionKey);
        Assert.Equal("my-token", capturedEntity.RowKey);
        Assert.NotEmpty(capturedEntity.SessionId);
    }

    [Fact]
    public async Task TryUseTokenAsync_OtherStorageError_Throws()
    {
        var mockTableClient = new Mock<TableClient>();
        mockTableClient
            .Setup(x => x.AddEntityAsync(
                It.IsAny<UsedTokenEntity>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(500, "Internal server error"));

        var service = new TestableTokenStoreService(mockTableClient.Object);

        await Assert.ThrowsAsync<RequestFailedException>(
            () => service.TryUseTokenAsync("room-1", "token-1", null));
    }
}

/// <summary>
/// Testable version of TokenStoreService that accepts a mock TableClient.
/// </summary>
public class TestableTokenStoreService : ITokenStoreService
{
    private readonly TableClient tableClient;

    public TestableTokenStoreService(TableClient tableClient)
    {
        this.tableClient = tableClient;
    }

    public async Task<TokenUseResult> TryUseTokenAsync(string roomId, string tokenId, string? sessionId)
    {
        var newSessionId = Guid.NewGuid().ToString("N");

        var entity = new UsedTokenEntity
        {
            PartitionKey = roomId,
            RowKey = tokenId,
            UsedAt = DateTimeOffset.UtcNow,
            SessionId = newSessionId
        };

        try
        {
            await this.tableClient.AddEntityAsync(entity);
            return new TokenUseResult(Allowed: true, SessionId: newSessionId);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                var existing = await this.tableClient.GetEntityAsync<UsedTokenEntity>(roomId, tokenId);
                if (existing.Value.SessionId == sessionId)
                {
                    return new TokenUseResult(Allowed: true, SessionId: sessionId);
                }
            }

            return new TokenUseResult(Allowed: false, SessionId: "");
        }
    }
}
