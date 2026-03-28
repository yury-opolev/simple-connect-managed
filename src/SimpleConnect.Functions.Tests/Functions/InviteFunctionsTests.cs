using Microsoft.Extensions.Logging;
using Moq;
using SimpleConnect.Functions.Configuration;
using SimpleConnect.Functions.Functions;
using SimpleConnect.Functions.Services;

namespace SimpleConnect.Functions.Tests.Functions;

public class InviteFunctionsTests
{
    private readonly Mock<IInviteTokenService> mockTokenService;
    private readonly Mock<IRoomStoreService> mockRoomStore;
    private readonly AppSettings settings;
    private readonly Mock<ILogger<InviteFunctions>> mockLogger;
    private readonly InviteFunctions function;

    public InviteFunctionsTests()
    {
        this.mockTokenService = new Mock<IInviteTokenService>();
        this.mockRoomStore = new Mock<IRoomStoreService>();
        this.settings = new AppSettings
        {
            BaseUrl = "https://test.azurewebsites.net",
            TokenExpiryHours = 24
        };
        this.mockLogger = new Mock<ILogger<InviteFunctions>>();

        this.function = new InviteFunctions(
            this.mockTokenService.Object,
            this.mockRoomStore.Object,
            this.settings,
            this.mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        Assert.NotNull(this.function);
    }
}
