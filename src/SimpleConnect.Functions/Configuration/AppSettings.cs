namespace SimpleConnect.Functions.Configuration;

public class AppSettings
{
    public string AcsConnectionString { get; set; } = "";
    public string StorageConnectionString { get; set; } = "";
    public string JwtSecret { get; set; } = "";
    public int TokenExpiryHours { get; set; } = 24;
    public string BaseUrl { get; set; } = "";
    public string EntraTenantId { get; set; } = "";
    public string EntraClientId { get; set; } = "";
    public HashSet<string> AdminObjectIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
