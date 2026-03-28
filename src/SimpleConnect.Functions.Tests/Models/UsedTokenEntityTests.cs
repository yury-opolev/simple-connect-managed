using SimpleConnect.Functions.Models;

namespace SimpleConnect.Functions.Tests.Models;

public class UsedTokenEntityTests
{
    [Fact]
    public void DefaultValues_AreEmptyStrings()
    {
        var entity = new UsedTokenEntity();

        Assert.Equal("", entity.PartitionKey);
        Assert.Equal("", entity.RowKey);
    }

    [Fact]
    public void UsedAt_DefaultsToCurrentTime()
    {
        var before = DateTimeOffset.UtcNow;
        var entity = new UsedTokenEntity();
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(entity.UsedAt, before, after);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new UsedTokenEntity
        {
            PartitionKey = "room-1",
            RowKey = "token-1",
            UsedAt = now
        };

        Assert.Equal("room-1", entity.PartitionKey);
        Assert.Equal("token-1", entity.RowKey);
        Assert.Equal(now, entity.UsedAt);
    }

    [Fact]
    public void ImplementsITableEntity()
    {
        var entity = new UsedTokenEntity();

        Assert.IsAssignableFrom<Azure.Data.Tables.ITableEntity>(entity);
    }
}
