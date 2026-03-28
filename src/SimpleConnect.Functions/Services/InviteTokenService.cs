using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SimpleConnect.Functions.Configuration;

namespace SimpleConnect.Functions.Services;

public class InviteTokenService : IInviteTokenService
{
    private readonly AppSettings settings;
    private readonly JwtSecurityTokenHandler tokenHandler = new();

    public InviteTokenService(AppSettings settings)
    {
        this.settings = settings;
    }

    public GeneratedToken GenerateInviteToken(string roomId, int? expiryHours = null, string? guestName = null)
    {
        var hours = expiryHours ?? this.settings.TokenExpiryHours;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.settings.JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenId = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, roomId),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new("role", "guest")
        };

        if (!string.IsNullOrWhiteSpace(guestName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, guestName));
        }

        var token = new JwtSecurityToken(
            issuer: "simple-connect",
            audience: "simple-connect-guest",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: credentials);

        return new GeneratedToken(this.tokenHandler.WriteToken(token), tokenId);
    }

    public InviteTokenClaims? ValidateInviteToken(string token, string expectedRoomId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.settings.JwtSecret));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "simple-connect",
            ValidateAudience = true,
            ValidAudience = "simple-connect-guest",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            var principal = this.tokenHandler.ValidateToken(token, validationParameters, out _);

            var roomId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var tokenId = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);

            if (roomId != expectedRoomId || string.IsNullOrEmpty(tokenId))
            {
                return null;
            }

            var guestName = principal.FindFirstValue(JwtRegisteredClaimNames.GivenName)
                ?? principal.FindFirstValue(ClaimTypes.GivenName);

            return new InviteTokenClaims(roomId, tokenId, guestName);
        }
        catch
        {
            return null;
        }
    }
}
