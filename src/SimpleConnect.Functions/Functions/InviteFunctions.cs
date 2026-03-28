using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SimpleConnect.Functions.Configuration;
using SimpleConnect.Functions.Models;
using SimpleConnect.Functions.Services;

namespace SimpleConnect.Functions.Functions;

public class InviteFunctions
{
    private readonly IInviteTokenService tokenService;
    private readonly IRoomStoreService roomStore;
    private readonly AppSettings settings;
    private readonly ILogger<InviteFunctions> logger;

    public InviteFunctions(
        IInviteTokenService tokenService,
        IRoomStoreService roomStore,
        AppSettings settings,
        ILogger<InviteFunctions> logger)
    {
        this.tokenService = tokenService;
        this.roomStore = roomStore;
        this.settings = settings;
        this.logger = logger;
    }

    [Function("CreateInvite")]
    public async Task<HttpResponseData> CreateInvite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/rooms/{roomId}/invites")] HttpRequestData req,
        string roomId,
        FunctionContext context)
    {
        if (!AuthHelper.IsAdmin(context, this.settings))
        {
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }

        var body = await req.ReadFromJsonAsync<CreateInviteRequest>();
        var expiryHours = body?.ExpiryHours;

        var generated = this.tokenService.GenerateInviteToken(roomId, expiryHours, body?.GuestName);
        var baseUrl = this.settings.BaseUrl.TrimEnd('/');
        var inviteUrl = $"{baseUrl}/join?roomId={roomId}&token={generated.Token}";

        var expiresAt = DateTimeOffset.UtcNow.AddHours(expiryHours ?? this.settings.TokenExpiryHours);
        var shortCode = await this.roomStore.SaveInviteAsync(roomId, generated.TokenId, body?.GuestName ?? "", inviteUrl, expiresAt);
        var shortInviteUrl = $"{baseUrl}/s/{shortCode}";

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new CreateInviteResponse(
            InviteUrl: inviteUrl,
            ShortInviteUrl: shortInviteUrl,
            ExpiresAt: expiresAt));

        await this.roomStore.IncrementInviteCountAsync(roomId);
        this.logger.LogInformation("Invite created for room {RoomId}, expires at {ExpiresAt}", roomId, expiresAt);
        return response;
    }
}

public record CreateInviteRequest(int? ExpiryHours = null, string? GuestName = null);
