using Azure;
using Azure.Data.Tables;

namespace SimpleConnect.Functions.Models;

public class RoomEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "ROOM";
    public string RowKey { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public int InviteCount { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
