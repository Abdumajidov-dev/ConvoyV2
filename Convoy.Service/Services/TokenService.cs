using Convoy.Data.DbContexts;
using Convoy.Domain.Entities;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Convoy.Service.Services;

public class TokenService : ITokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationHours;
    private readonly AppDbConText _dbContext;

    public TokenService(IConfiguration configuration, AppDbConText dbContext)
    {
        _secretKey = configuration["Jwt:SecretKey"] ?? "your-256-bit-secret-key-here-make-it-long-and-secure";
        _issuer = configuration["Jwt:Issuer"] ?? "ConvoyApi";
        _audience = configuration["Jwt:Audience"] ?? "ConvoyClients";
        _expirationHours = int.TryParse(configuration["Jwt:ExpirationHours"], out var hours) ? hours : 24;
        _dbContext = dbContext;
    }

    public string GenerateToken(PhpWorkerDto worker)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, worker.WorkerId.ToString()),
            new Claim(ClaimTypes.Name, worker.WorkerName),
            new Claim(ClaimTypes.MobilePhone, worker.PhoneNumber),
            new Claim("worker_guid", worker.WorkerGuid),
            new Claim("branch_guid", worker.BranchGuid),
            new Claim("branch_name", worker.BranchName),
            new Claim("position_id", worker.PositionId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_expirationHours),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public int? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = false, // DEVELOPMENT: sistemaning soati noto'g'ri bo'lganda
                ClockSkew = TimeSpan.FromDays(365) // 1 yilgacha clock skew ruxsat berish
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            // Claim turlarini tekshirish - JWT da "nameid" deb saqlanadi
            var workerIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "nameid" || x.Type == ClaimTypes.NameIdentifier);

            if (workerIdClaim == null)
                return null;

            return int.Parse(workerIdClaim.Value);
        }
        catch (Exception ex)
        {
            // DEVELOPMENT: Exception log
            Console.WriteLine($"🔴 Token validation error: {ex.Message}");
            Console.WriteLine($"🔴 Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    public long? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        // JWT middleware tomonidan yaratilgan ClaimsPrincipal'dan user ID ni olish
        // ClaimTypes.NameIdentifier JWT'da "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" deb saqlanadi
        // Lekin tokenni deserialize qilganda "nameid" ga qisqartiriladi
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)  // Standart claim type
                       ?? user.FindFirst("nameid")                   // JWT qisqartirilgan format
                       ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"); // To'liq format

        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            return null;

        if (long.TryParse(userIdClaim.Value, out long userId))
            return userId;

        return null;
    }

    public string? GetJtiFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch
        {
            return null;
        }
    }

    public DateTime? GetExpiryFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        if (string.IsNullOrEmpty(jti))
            return false;

        return await _dbContext.TokenBlacklists
            .AnyAsync(tb => tb.TokenJti == jti && tb.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<bool> BlacklistTokenAsync(string token, long userId, string reason = "logout")
    {
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            var jti = GetJtiFromToken(token);
            var expiry = GetExpiryFromToken(token);

            if (jti == null || expiry == null)
                return false;

            // Tekshirish - allaqachon blacklist'da bormi
            var exists = await _dbContext.TokenBlacklists
                .AnyAsync(tb => tb.TokenJti == jti);

            if (exists)
                return true; // Allaqachon blacklist'da

            // Yangi blacklist entry yaratish
            var blacklistEntry = new TokenBlacklist
            {
                TokenJti = jti,
                UserId = userId,
                BlacklistedAt = DateTime.UtcNow,
                ExpiresAt = expiry.Value,
                Reason = reason
            };

            _dbContext.TokenBlacklists.Add(blacklistEntry);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }
}
