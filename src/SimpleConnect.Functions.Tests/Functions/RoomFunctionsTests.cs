using Microsoft.Extensions.Logging;
using Moq;
using SimpleConnect.Functions.Configuration;
using SimpleConnect.Functions.Functions;
using SimpleConnect.Functions.Services;

namespace SimpleConnect.Functions.Tests.Functions;

public class RoomFunctionsTests
{
    private readonly Mock<IRoomService> mockRoomService;
    private readonly Mock<IRoomStoreService> mockRoomStore;
    private readonly Mock<IInviteTokenService> mockTokenService;
    private readonly AppSettings settings;
    private readonly Mock<ILogger<RoomFunctions>> mockLogger;
    private readonly RoomFunctions function;

    public RoomFunctionsTests()
    {
        this.mockRoomService = new Mock<IRoomService>();
        this.mockRoomStore = new Mock<IRoomStoreService>();
        this.mockTokenService = new Mock<IInviteTokenService>();
        this.settings = new AppSettings
        {
            BaseUrl = "https://test.azurewebsites.net",
            TokenExpiryHours = 24
        };
        this.mockLogger = new Mock<ILogger<RoomFunctions>>();

        this.function = new RoomFunctions(
            this.mockRoomService.Object,
            this.mockRoomStore.Object,
            this.mockTokenService.Object,
            this.settings,
            this.mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        Assert.NotNull(this.function);
    }
}
