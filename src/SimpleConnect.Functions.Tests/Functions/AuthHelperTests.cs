using SimpleConnect.Functions.Configuration;
using SimpleConnect.Functions.Functions;

namespace SimpleConnect.Functions.Tests.Functions;

public class AuthHelperTests
{
    [Fact]
    public void AdminObjectIds_CaseInsensitive()
    {
        var settings = new AppSettings
        {
            AdminObjectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "abc-123-def"
            }
        };

        Assert.True(settings.AdminObjectIds.Contains("ABC-123-DEF"));
        Assert.True(settings.AdminObjectIds.Contains("abc-123-def"));
        Assert.False(settings.AdminObjectIds.Contains("xyz-456"));
    }

    [Fact]
    public void AdminObjectIds_EmptySet_ContainsNothing()
    {
        var settings = new AppSettings
        {
            AdminObjectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        Assert.False(settings.AdminObjectIds.Contains("any-value"));
    }

    [Fact]
    public void AdminObjectIds_MultipleEntries()
    {
        var settings = new AppSettings
        {
            AdminObjectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "user-1",
                "user-2",
                "user-3"
            }
        };

        Assert.True(settings.AdminObjectIds.Contains("user-1"));
        Assert.True(settings.AdminObjectIds.Contains("user-2"));
        Assert.True(settings.AdminObjectIds.Contains("user-3"));
        Assert.False(settings.AdminObjectIds.Contains("user-4"));
    }

    [Fact]
    public void AdminObjectIds_ParseFromCommaSeparatedString()
    {
        var raw = "  user-1 , user-2 , user-3  ";
        var parsed = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(3, parsed.Count);
        Assert.True(parsed.Contains("user-1"));
        Assert.True(parsed.Contains("user-2"));
        Assert.True(parsed.Contains("user-3"));
    }

    [Fact]
    public void AdminObjectIds_EmptyString_ParsesAsEmptySet()
    {
        var raw = "";
        var parsed = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Empty(parsed);
    }
}
