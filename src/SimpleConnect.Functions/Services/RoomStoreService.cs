using System.Security.Cryptography;
using Azure.Data.Tables;
using SimpleConnect.Functions.Configuration;
using SimpleConnect.Functions.Models;

namespace SimpleConnect.Functions.Services;

public class RoomStoreService : IRoomStoreService
{
    private const string TableName = "ActiveRooms";
    private const string InvitesTableName = "RoomInvites";
    private const string ShortLinksTableName = "ShortLinks";
    private const string Partition = "ROOM";
    private const string ShortLinkPartition = "LINK";
    private const string ShortCodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private readonly TableClient tableClient;
    private readonly TableClient invitesTableClient;
    private readonly TableClient shortLinksTableClient;

    public RoomStoreService(AppSettings settings)
    {
        var serviceClient = new TableServiceClient(settings.StorageConnectionString);
        this.tableClient = serviceClient.GetTableClient(TableName);
        this.tableClient.CreateIfNotExists();
        this.invitesTableClient = serviceClient.GetTableClient(InvitesTableName);
        this.invitesTableClient.CreateIfNotExists();
        this.shortLinksTableClient = serviceClient.GetTableClient(ShortLinksTableName);
        this.shortLinksTableClient.CreateIfNotExists();
    }

    public async Task SaveRoomAsync(string roomId, DateTimeOffset expiresAt)
    {
        var entity = new RoomEntity
        {
            PartitionKey = Partition,
            RowKey = roomId,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            InviteCount = 0
        };

        await this.tableClient.UpsertEntityAsync(entity);
    }

    public async Task<List<RoomEntity>> GetActiveRoomsAsync()
    {
        var rooms = new List<RoomEntity>();
        var now = DateTimeOffset.UtcNow;

        await foreach (var entity in this.tableClient.QueryAsync<RoomEntity>(e => e.PartitionKey == Partition))
        {
            if (entity.ExpiresAt > now)
            {
                rooms.Add(entity);
            }
        }

        return rooms;
    }

    public async Task DeleteRoomRecordAsync(string roomId)
    {
        await this.tableClient.DeleteEntityAsync(Partition, roomId);
    }

    public async Task IncrementInviteCountAsync(string roomId)
    {
        var response = await this.tableClient.GetEntityAsync<RoomEntity>(Partition, roomId);
        var entity = response.Value;
        entity.InviteCount++;
        await this.tableClient.UpdateEntityAsync(entity, Azure.ETag.All);
    }

    public async Task<string> SaveInviteAsync(string roomId, string tokenId, string guestName, string inviteUrl, DateTimeOffset expiresAt)
    {
        var shortCode = GenerateShortCode();

        var invite = new InviteEntity
        {
            PartitionKey = roomId,
            RowKey = tokenId,
            GuestName = guestName,
            InviteUrl = inviteUrl,
            ShortCode = shortCode,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        };
        await this.invitesTableClient.UpsertEntityAsync(invite);

        var shortLink = new ShortLinkEntity
        {
            PartitionKey = ShortLinkPartition,
            RowKey = shortCode,
            InviteUrl = inviteUrl,
            RoomId = roomId
        };
        await this.shortLinksTableClient.UpsertEntityAsync(shortLink);

        return shortCode;
    }

    public async Task<List<InviteEntity>> GetInvitesForRoomAsync(string roomId)
    {
        var invites = new List<InviteEntity>();

        await foreach (var entity in this.invitesTableClient.QueryAsync<InviteEntity>(e => e.PartitionKey == roomId))
        {
            invites.Add(entity);
        }

        return invites;
    }

    public async Task<string?> GetShortLinkUrlAsync(string code)
    {
        try
        {
            var response = await this.shortLinksTableClient.GetEntityAsync<ShortLinkEntity>(ShortLinkPartition, code);
            return response.Value.InviteUrl;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    private static string GenerateShortCode(int length = 16)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return new string(bytes.Select(b => ShortCodeChars[b % ShortCodeChars.Length]).ToArray());
    }
}
