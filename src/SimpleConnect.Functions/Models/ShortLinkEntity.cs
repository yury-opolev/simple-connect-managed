using Azure;
using Azure.Data.Tables;

namespace SimpleConnect.Functions.Models;

public class ShortLinkEntity : ITableEntity
{
    /// <summary>PartitionKey = "LINK" (constant)</summary>
    public string PartitionKey { get; set; } = "LINK";

    /// <summary>RowKey = Short code (16 alphanumeric characters)</summary>
    public string RowKey { get; set; } = "";

    public string InviteUrl { get; set; } = "";
    public string RoomId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
