using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace SimpleConnect.Client.Services;

public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(string message, int statusCode) : base(message)
    {
        this.StatusCode = statusCode;
    }
}

public class ApiClient : IApiClient
{
    private readonly HttpClient httpClient;
    private readonly IAccessTokenProvider tokenProvider;

    public ApiClient(HttpClient httpClient, IAccessTokenProvider tokenProvider)
    {
        this.httpClient = httpClient;
        this.tokenProvider = tokenProvider;
    }

    public async Task<RoomInfo> CreateRoomAsync(int durationHours, string? guestName1 = null, string? guestName2 = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/rooms");
        await this.AttachBearerTokenAsync(request);
        request.Content = JsonContent.Create(new { DurationHours = durationHours, GuestName1 = guestName1, GuestName2 = guestName2 });

        var response = await this.httpClient.SendAsync(request);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<RoomInfo>()
            ?? throw new ApiException("Failed to parse room response", (int)response.StatusCode);
    }

    public async Task<InviteInfo> CreateInviteAsync(string roomId, int expiryHours, string? guestName = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/rooms/{roomId}/invites");
        await this.AttachBearerTokenAsync(request);
        request.Content = JsonContent.Create(new { ExpiryHours = expiryHours, GuestName = guestName });

        var response = await this.httpClient.SendAsync(request);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<InviteInfo>()
            ?? throw new ApiException("Failed to parse invite response", (int)response.StatusCode);
    }

    public async Task<JoinInfo> JoinRoomAsync(string roomId, string token, string? sessionId = null)
    {
        var url = $"api/rooms/{roomId}/join?token={Uri.EscapeDataString(token)}";
        if (!string.IsNullOrEmpty(sessionId))
        {
            url += $"&sessionId={Uri.EscapeDataString(sessionId)}";
        }
        var response = await this.httpClient.GetAsync(url);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<JoinInfo>()
            ?? throw new ApiException("Failed to parse join response", (int)response.StatusCode);
    }

    public async Task DeleteRoomAsync(string roomId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/rooms/{roomId}");
        await this.AttachBearerTokenAsync(request);

        var response = await this.httpClient.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    public async Task<List<RoomSummary>> GetActiveRoomsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/rooms");
        await this.AttachBearerTokenAsync(request);

        var response = await this.httpClient.SendAsync(request);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<List<RoomSummary>>()
            ?? new List<RoomSummary>();
    }

    public async Task<RejoinInfo> RejoinRoomAsync(string roomId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/rooms/{roomId}/rejoin");
        await this.AttachBearerTokenAsync(request);

        var response = await this.httpClient.SendAsync(request);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<RejoinInfo>()
            ?? throw new ApiException("Failed to parse rejoin response", (int)response.StatusCode);
    }

    private async Task AttachBearerTokenAsync(HttpRequestMessage request)
    {
        var tokenResult = await this.tokenProvider.RequestAccessToken();
        if (tokenResult.TryGetToken(out var accessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Value);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new ApiException(
                $"API error: {response.StatusCode} - {body}",
                (int)response.StatusCode);
        }
    }
}
