using Azure;
using Azure.Data.Tables;
using SimpleConnect.Functions.Configuration;
using SimpleConnect.Functions.Models;

namespace SimpleConnect.Functions.Services;

public class TokenStoreService : ITokenStoreService
{
    private const string TableName = "UsedTokens";
    private readonly TableClient tableClient;

    public TokenStoreService(AppSettings settings)
    {
        var serviceClient = new TableServiceClient(settings.StorageConnectionString);
        this.tableClient = serviceClient.GetTableClient(TableName);
        this.tableClient.CreateIfNotExists();
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
            // Token already used — check if same session is reconnecting
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
