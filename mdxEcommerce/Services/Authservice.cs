using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using mdxEcommerce.Data;
using mdxEcommerce.DTOs;
using mdxEcommerce.Models;

namespace mdxEcommerce.Services;

public class AuthService(ApplicationDbContext db, IConfiguration config)
{
    private readonly string _jwtKey      = config["Jwt:Key"]      ?? throw new InvalidOperationException("JWT Key no configurada.");
    private readonly string _jwtIssuer   = config["Jwt:Issuer"]   ?? "mdxEcommerce";
    private readonly string _jwtAudience = config["Jwt:Audience"] ?? "mdxEcommerce";
    private readonly int    _accessTokenMinutes  = int.TryParse(config["Jwt:AccessTokenExpirationMinutes"], out var m) ? m : 15;
    private readonly int    _refreshTokenDays    = int.TryParse(config["Jwt:RefreshTokenExpirationDays"],  out var d) ? d : 7;

    // ── Registro ──────────────────────────────────────────────────────────
    public async Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            return (false, "El email ya está registrado.", null);

        if (await db.Users.AnyAsync(u => u.Username == request.Username.ToLower()))
            return (false, "El nombre de usuario ya está en uso.", null);

        var user = new User
        {
            Email        = request.Email.ToLower().Trim(),
            Username     = request.Username.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            FirstName    = request.FirstName,
            LastName     = request.LastName,
            Role         = Role.Customer
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (accessToken, refreshToken) = await GenerateTokensAsync(user);
        return (true, null, BuildResponse(user, accessToken, refreshToken));
    }

    // ── Login ─────────────────────────────────────────────────────────────
    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (false, "Email o contraseña incorrectos.", null);

        if (!user.IsActive)
            return (false, "La cuenta está deshabilitada.", null);

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var (accessToken, refreshToken) = await GenerateTokensAsync(user);
        return (true, null, BuildResponse(user, accessToken, refreshToken));
    }

    // ── Refresh ───────────────────────────────────────────────────────────
    public async Task<(bool Success, string? Error, AuthResponse? Response)> RefreshAsync(string rawRefreshToken)
    {
        var tokenHash = HashToken(rawRefreshToken);

        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return (false, "Refresh token inválido o expirado.", null);

        // Rotar: revocar el anterior y emitir uno nuevo
        stored.IsRevoked = true;
        var (accessToken, newRefreshToken) = await GenerateTokensAsync(stored.User);

        return (true, null, BuildResponse(stored.User, accessToken, newRefreshToken));
    }

    // ── Logout ────────────────────────────────────────────────────────────
    public async Task<bool> LogoutAsync(string rawRefreshToken)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (stored is null) return false;

        stored.IsRevoked = true;
        await db.SaveChangesAsync();
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private async Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(User user)
    {
        var accessToken   = GenerateAccessToken(user);
        var rawRefresh    = GenerateRawRefreshToken();
        var hashedRefresh = HashToken(rawRefresh);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId    = user.Id,
            TokenHash = hashedRefresh,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays)
        });

        await db.SaveChangesAsync();
        return (accessToken, rawRefresh);
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name,               user.Username),
            new Claim(ClaimTypes.Role,               user.Role.ToString())
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _jwtIssuer,
            audience:           _jwtAudience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRawRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLower();
    }

    private static AuthResponse BuildResponse(User user, string accessToken, string refreshToken)
        => new(accessToken, refreshToken, user.Email, user.Username, user.Role.ToString());
}