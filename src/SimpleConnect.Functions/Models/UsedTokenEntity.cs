using Azure;
using Azure.Data.Tables;

namespace SimpleConnect.Functions.Models;

public class UsedTokenEntity : ITableEntity
{
    /// <summary>PartitionKey = RoomId</summary>
    public string PartitionKey { get; set; } = "";

    /// <summary>RowKey = Token JTI (unique token identifier)</summary>
    public string RowKey { get; set; } = "";

    public DateTimeOffset UsedAt { get; set; } = DateTimeOffset.UtcNow;
    public string SessionId { get; set; } = "";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
