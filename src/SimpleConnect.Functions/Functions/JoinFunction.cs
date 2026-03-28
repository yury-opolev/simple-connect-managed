using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SimpleConnect.Functions.Models;
using SimpleConnect.Functions.Services;

namespace SimpleConnect.Functions.Functions;

public class JoinFunction
{
    private readonly IInviteTokenService tokenService;
    private readonly ITokenStoreService tokenStore;
    private readonly IRoomService roomService;
    private readonly ILogger<JoinFunction> logger;

    public JoinFunction(
        IInviteTokenService tokenService,
        ITokenStoreService tokenStore,
        IRoomService roomService,
        ILogger<JoinFunction> logger)
    {
        this.tokenService = tokenService;
        this.tokenStore = tokenStore;
        this.roomService = roomService;
        this.logger = logger;
    }

    [Function("JoinRoom")]
    public async Task<HttpResponseData> JoinRoom(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/rooms/{roomId}/join")] HttpRequestData req,
        string roomId)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var token = query["token"];
        var sessionId = query["sessionId"];

        if (string.IsNullOrEmpty(token))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Missing invite token" });
            return badRequest;
        }

        var claims = this.tokenService.ValidateInviteToken(token, roomId);
        if (claims is null)
        {
            this.logger.LogWarning("Invalid or expired invite token for room {RoomId}", roomId);
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = "Invalid or expired invite token" });
            return forbidden;
        }

        var useResult = await this.tokenStore.TryUseTokenAsync(roomId, claims.TokenId, sessionId);
        if (!useResult.Allowed)
        {
            this.logger.LogWarning("Invite token {TokenId} for room {RoomId} used by different session", claims.TokenId, roomId);
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = "This invite link has already been used" });
            return forbidden;
        }

        var result = await this.roomService.AddGuestToRoomAsync(roomId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new JoinRoomResponse(
            AcsToken: result.AcsToken,
            AcsUserId: result.AcsUserId,
            RoomId: roomId,
            DisplayName: claims.GuestName,
            SessionId: useResult.SessionId));

        this.logger.LogInformation("Guest joined room {RoomId} (session {SessionId})", roomId, useResult.SessionId);
        return response;
    }
}
