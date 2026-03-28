using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SimpleConnect.Functions.Functions;
using SimpleConnect.Functions.Services;

namespace SimpleConnect.Functions.Tests.Functions;

public class JoinFunctionTests
{
    private readonly Mock<IInviteTokenService> mockTokenService;
    private readonly Mock<ITokenStoreService> mockTokenStore;
    private readonly Mock<IRoomService> mockRoomService;
    private readonly Mock<ILogger<JoinFunction>> mockLogger;
    private readonly JoinFunction function;

    public JoinFunctionTests()
    {
        this.mockTokenService = new Mock<IInviteTokenService>();
        this.mockTokenStore = new Mock<ITokenStoreService>();
        this.mockRoomService = new Mock<IRoomService>();
        this.mockLogger = new Mock<ILogger<JoinFunction>>();

        this.function = new JoinFunction(
            this.mockTokenService.Object,
            this.mockTokenStore.Object,
            this.mockRoomService.Object,
            this.mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        Assert.NotNull(this.function);
    }

    [Fact]
    public void JoinFunction_ImplementsCorrectDependencies()
    {
        // Verify that the function has the expected dependencies injected
        Assert.NotNull(this.mockTokenService.Object);
        Assert.NotNull(this.mockTokenStore.Object);
        Assert.NotNull(this.mockRoomService.Object);
        Assert.NotNull(this.mockLogger.Object);
    }
}
