using SimpleConnect.Functions.Configuration;

namespace SimpleConnect.Functions.Tests.Configuration;

public class AppSettingsTests
{
    [Fact]
    public void DefaultValues_AreEmptyStrings()
    {
        var settings = new AppSettings();

        Assert.Equal("", settings.AcsConnectionString);
        Assert.Equal("", settings.StorageConnectionString);
        Assert.Equal("", settings.JwtSecret);
        Assert.Equal("", settings.BaseUrl);
        Assert.Equal("", settings.EntraTenantId);
        Assert.Equal("", settings.EntraClientId);
    }

    [Fact]
    public void DefaultTokenExpiryHours_Is24()
    {
        var settings = new AppSettings();

        Assert.Equal(24, settings.TokenExpiryHours);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var settings = new AppSettings
        {
            AcsConnectionString = "acs-conn",
            StorageConnectionString = "storage-conn",
            JwtSecret = "my-secret",
            TokenExpiryHours = 48,
            BaseUrl = "https://example.com",
            EntraTenantId = "tenant-id",
            EntraClientId = "client-id"
        };

        Assert.Equal("acs-conn", settings.AcsConnectionString);
        Assert.Equal("storage-conn", settings.StorageConnectionString);
        Assert.Equal("my-secret", settings.JwtSecret);
        Assert.Equal(48, settings.TokenExpiryHours);
        Assert.Equal("https://example.com", settings.BaseUrl);
        Assert.Equal("tenant-id", settings.EntraTenantId);
        Assert.Equal("client-id", settings.EntraClientId);
    }
}
