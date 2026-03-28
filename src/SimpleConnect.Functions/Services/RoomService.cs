using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using SimpleConnect.Functions.Configuration;

namespace SimpleConnect.Functions.Services;

public class RoomService : IRoomService
{
    private readonly CommunicationIdentityClient identityClient;
    private readonly RoomsClient roomsClient;
    private readonly AppSettings settings;

    public RoomService(AppSettings settings)
    {
        this.settings = settings;
        this.identityClient = new CommunicationIdentityClient(settings.AcsConnectionString);
        this.roomsClient = new RoomsClient(settings.AcsConnectionString);
    }

    public async Task<CreateRoomResult> CreateRoomAsync(int? roomDurationHours = null)
    {
        var duration = roomDurationHours ?? this.settings.TokenExpiryHours;

        var adminIdentity = await this.identityClient.CreateUserAsync();
        var adminToken = await this.identityClient.GetTokenAsync(
            adminIdentity.Value,
            scopes: new[] { CommunicationTokenScope.VoIP });

        var validFrom = DateTimeOffset.UtcNow;
        var validUntil = validFrom.AddHours(duration);

        var participant = new RoomParticipant(adminIdentity.Value)
        {
            Role = ParticipantRole.Presenter
        };

        var room = await this.roomsClient.CreateRoomAsync(
            validFrom: validFrom,
            validUntil: validUntil,
            participants: new[] { participant });

        return new CreateRoomResult(
            RoomId: room.Value.Id,
            AdminAcsToken: adminToken.Value.Token,
            AdminUserId: adminIdentity.Value.Id,
            ExpiresAt: validUntil);
    }

    public async Task<JoinRoomResult> AddGuestToRoomAsync(string roomId)
    {
        var guestIdentity = await this.identityClient.CreateUserAsync();
        var guestToken = await this.identityClient.GetTokenAsync(
            guestIdentity.Value,
            scopes: new[] { CommunicationTokenScope.VoIP });

        var participant = new RoomParticipant(guestIdentity.Value)
        {
            Role = ParticipantRole.Attendee
        };

        await this.roomsClient.AddOrUpdateParticipantsAsync(
            roomId,
            new[] { participant });

        return new JoinRoomResult(
            AcsToken: guestToken.Value.Token,
            AcsUserId: guestIdentity.Value.Id);
    }

    public async Task<CreateRoomResult> RejoinRoomAsAdminAsync(string roomId)
    {
        var room = await this.roomsClient.GetRoomAsync(roomId);

        var adminIdentity = await this.identityClient.CreateUserAsync();
        var adminToken = await this.identityClient.GetTokenAsync(
            adminIdentity.Value,
            scopes: new[] { CommunicationTokenScope.VoIP });

        var participant = new RoomParticipant(adminIdentity.Value)
        {
            Role = ParticipantRole.Presenter
        };

        await this.roomsClient.AddOrUpdateParticipantsAsync(roomId, new[] { participant });

        return new CreateRoomResult(
            RoomId: roomId,
            AdminAcsToken: adminToken.Value.Token,
            AdminUserId: adminIdentity.Value.Id,
            ExpiresAt: room.Value.ValidUntil);
    }

    public async Task DeleteRoomAsync(string roomId)
    {
        await this.roomsClient.DeleteRoomAsync(roomId);
    }
}
