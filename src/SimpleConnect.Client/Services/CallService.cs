using Microsoft.JSInterop;

namespace SimpleConnect.Client.Services;

public class CallService : ICallService
{
    private readonly IJSRuntime jsRuntime;

    public CallService(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync(string acsToken, string displayName)
    {
        await this.jsRuntime.InvokeVoidAsync("AcsInterop.initCallAgent", acsToken, displayName);
    }

    public async Task JoinRoomAsync(string roomId)
    {
        await this.jsRuntime.InvokeVoidAsync("AcsInterop.joinRoom", roomId);
    }

    public async Task SetupLocalVideoAsync(string containerId)
    {
        await this.jsRuntime.InvokeVoidAsync("AcsInterop.setupLocalVideo", containerId);
    }

    public async Task SubscribeToParticipantsAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class
    {
        await this.jsRuntime.InvokeVoidAsync("AcsInterop.subscribeRemoteParticipants", dotNetRef);
    }

    public async Task RenderRemoteVideoAsync(string participantId, string containerId)
    {
        await this.jsRuntime.InvokeVoidAsync("AcsInterop.renderRemoteStream", participantId, containerId);
    }

    public async Task ToggleMuteAsync()
    {
        await this.jsRuntime.InvokeVoidAsync("AcsInterop.toggleMute");
    }

    public async Task ToggleCameraAsync()
    {
        await this.jsRuntime.InvokeVoidAsync("AcsInterop.toggleCamera");
    }

    public async Task HangUpAsync()
    {
        await this.jsRuntime.InvokeVoidAsync("AcsInterop.hangUp");
    }
}
