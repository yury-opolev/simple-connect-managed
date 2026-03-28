using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SimpleConnect.Functions.Configuration;
using SimpleConnect.Functions.Models;
using SimpleConnect.Functions.Services;

namespace SimpleConnect.Functions.Functions;

public class RoomFunctions
{
    private readonly IRoomService roomService;
    private readonly IRoomStoreService roomStore;
    private readonly IInviteTokenService tokenService;
    private readonly AppSettings settings;
    private readonly ILogger<RoomFunctions> logger;

    public RoomFunctions(
        IRoomService roomService,
        IRoomStoreService roomStore,
        IInviteTokenService tokenService,
        AppSettings settings,
        ILogger<RoomFunctions> logger)
    {
        this.roomService = roomService;
        this.roomStore = roomStore;
        this.tokenService = tokenService;
        this.settings = settings;
        this.logger = logger;
    }

    [Function("ListRooms")]
    public async Task<HttpResponseData> ListRooms(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/rooms")] HttpRequestData req,
        FunctionContext context)
    {
        if (!AuthHelper.IsAdmin(context, this.settings))
        {
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }

        var rooms = await this.roomStore.GetActiveRoomsAsync();
        var baseUrl = this.settings.BaseUrl.TrimEnd('/');
        var summaries = new List<RoomSummary>();

        foreach (var r in rooms)
        {
            var invites = await this.roomStore.GetInvitesForRoomAsync(r.RowKey);
            var inviteSummaries = invites.Select(i => new InviteSummaryItem(
                GuestName: i.GuestName,
                ShortInviteUrl: !string.IsNullOrEmpty(i.ShortCode) ? $"{baseUrl}/s/{i.ShortCode}" : i.InviteUrl,
                CreatedAt: i.CreatedAt,
                ExpiresAt: i.ExpiresAt,
                Used: i.Used)).ToList();

            summaries.Add(new RoomSummary(
                RoomId: r.RowKey,
                CreatedAt: r.CreatedAt,
                ExpiresAt: r.ExpiresAt,
                InviteCount: r.InviteCount,
                Invites: inviteSummaries));
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(summaries);
        return response;
    }

    [Function("CreateRoom")]
    public async Task<HttpResponseData> CreateRoom(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/rooms")] HttpRequestData req,
        FunctionContext context)
    {
        if (!AuthHelper.IsAdmin(context, this.settings))
        {
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }

        var body = await req.ReadFromJsonAsync<CreateRoomRequest>();

        var durationHours = body?.DurationHours;
        var result = await this.roomService.CreateRoomAsync(durationHours);
        await this.roomStore.SaveRoomAsync(result.RoomId, result.ExpiresAt);

        var baseUrl = this.settings.BaseUrl.TrimEnd('/');
        var inviteExpiresAt = result.ExpiresAt;
        var guestNames = new[] { body?.GuestName1 ?? "", body?.GuestName2 ?? "" };
        var invites = new List<CreateRoomInvite>();

        foreach (var name in guestNames)
        {
            var generated = this.tokenService.GenerateInviteToken(result.RoomId, durationHours, name);
            var inviteUrl = $"{baseUrl}/join?roomId={result.RoomId}&token={generated.Token}";
            var shortCode = await this.roomStore.SaveInviteAsync(result.RoomId, generated.TokenId, name, inviteUrl, inviteExpiresAt);
            var shortInviteUrl = $"{baseUrl}/s/{shortCode}";
            await this.roomStore.IncrementInviteCountAsync(result.RoomId);
            invites.Add(new CreateRoomInvite(name, inviteUrl, shortInviteUrl));
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new CreateRoomResponse(
            RoomId: result.RoomId,
            Invites: invites,
            AdminAcsToken: result.AdminAcsToken,
            AdminUserId: result.AdminUserId,
            ExpiresAt: result.ExpiresAt));

        this.logger.LogInformation("Room {RoomId} created with {InviteCount} invites, expires at {ExpiresAt}", result.RoomId, invites.Count, result.ExpiresAt);
        return response;
    }

    [Function("ListInvitesForRoom")]
    public async Task<HttpResponseData> ListInvitesForRoom(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/rooms/{roomId}/invites")] HttpRequestData req,
        string roomId,
        FunctionContext context)
    {
        if (!AuthHelper.IsAdmin(context, this.settings))
        {
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }

        var invites = await this.roomStore.GetInvitesForRoomAsync(roomId);
        var baseUrl = this.settings.BaseUrl.TrimEnd('/');
        var summaries = invites.Select(i => new
        {
            i.GuestName,
            i.InviteUrl,
            ShortInviteUrl = !string.IsNullOrEmpty(i.ShortCode) ? $"{baseUrl}/s/{i.ShortCode}" : i.InviteUrl,
            i.CreatedAt,
            i.ExpiresAt,
            i.Used
        }).ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(summaries);
        return response;
    }

    [Function("RejoinRoom")]
    public async Task<HttpResponseData> RejoinRoom(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/rooms/{roomId}/rejoin")] HttpRequestData req,
        string roomId,
        FunctionContext context)
    {
        if (!AuthHelper.IsAdmin(context, this.settings))
        {
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }

        try
        {
            var result = await this.roomService.RejoinRoomAsAdminAsync(roomId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new RejoinRoomResponse(
                RoomId: result.RoomId,
                AdminAcsToken: result.AdminAcsToken,
                AdminUserId: result.AdminUserId,
                ExpiresAt: result.ExpiresAt));

            this.logger.LogInformation("Admin rejoined room {RoomId}", roomId);
            return response;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to rejoin room {RoomId}", roomId);
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "Room not found or expired" });
            return notFound;
        }
    }

    [Function("DeleteRoom")]
    public async Task<HttpResponseData> DeleteRoom(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "api/rooms/{roomId}")] HttpRequestData req,
        string roomId,
        FunctionContext context)
    {
        if (!AuthHelper.IsAdmin(context, this.settings))
        {
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }

        await this.roomService.DeleteRoomAsync(roomId);
        await this.roomStore.DeleteRoomRecordAsync(roomId);

        this.logger.LogInformation("Room {RoomId} deleted", roomId);
        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}

public record CreateRoomRequest(int? DurationHours = null, string? GuestName1 = null, string? GuestName2 = null);
