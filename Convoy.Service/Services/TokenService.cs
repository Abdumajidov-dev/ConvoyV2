using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
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

    public TokenService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"] ?? "your-256-bit-secret-key-here-make-it-long-and-secure";
        _issuer = configuration["Jwt:Issuer"] ?? "ConvoyApi";
        _audience = configuration["Jwt:Audience"] ?? "ConvoyClients";
        _expirationHours = int.TryParse(configuration["Jwt:ExpirationHours"], out var hours) ? hours : 24;
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
}
