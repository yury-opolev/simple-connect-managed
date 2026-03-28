using Azure;
using Azure.Data.Tables;

namespace SimpleConnect.Functions.Models;

public class InviteEntity : ITableEntity
{
    /// <summary>PartitionKey = RoomId</summary>
    public string PartitionKey { get; set; } = "";

    /// <summary>RowKey = Invite token JTI</summary>
    public string RowKey { get; set; } = "";

    public string GuestName { get; set; } = "";
    public string InviteUrl { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public string ShortCode { get; set; } = "";
    public bool Used { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
