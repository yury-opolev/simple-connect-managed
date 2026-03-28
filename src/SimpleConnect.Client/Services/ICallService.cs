using Microsoft.JSInterop;

namespace SimpleConnect.Client.Services;

public interface ICallService
{
    Task InitializeAsync(string acsToken, string displayName);
    Task JoinRoomAsync(string roomId);
    Task SetupLocalVideoAsync(string containerId);
    Task SubscribeToParticipantsAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class;
    Task RenderRemoteVideoAsync(string participantId, string containerId);
    Task ToggleMuteAsync();
    Task ToggleCameraAsync();
    Task HangUpAsync();
}
