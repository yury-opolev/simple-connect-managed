namespace SimpleConnect.Functions.Services;

public record TokenUseResult(bool Allowed, string SessionId);

public interface ITokenStoreService
{
    /// <summary>
    /// Attempts to use a token. Returns Allowed=true with a new SessionId on first use,
    /// or Allowed=true with the existing SessionId if the provided sessionId matches
    /// the original session (same browser reconnecting). Returns Allowed=false if
    /// the token was used by a different session.
    /// </summary>
    Task<TokenUseResult> TryUseTokenAsync(string roomId, string tokenId, string? sessionId);
}
