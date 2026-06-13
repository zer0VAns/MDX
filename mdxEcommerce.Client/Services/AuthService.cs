using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using mdxEcommerce.Client.Models;

namespace mdxEcommerce.Client.Services;

public class AuthService(HttpClient http, IJSRuntime js)
{
    private const string AccessTokenKey  = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";

    // ── API calls ─────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, error?.Message ?? "Error al iniciar sesión.");
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth is null) return (false, "Respuesta inválida del servidor.");

            await SaveTokensAsync(auth.AccessToken, auth.RefreshToken);
            return (true, null);
        }
        catch
        {
            return (false, "No se pudo conectar con el servidor.");
        }
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/register", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, error?.Message ?? "Error al registrarse.");
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth is null) return (false, "Respuesta inválida del servidor.");

            await SaveTokensAsync(auth.AccessToken, auth.RefreshToken);
            return (true, null);
        }
        catch
        {
            return (false, "No se pudo conectar con el servidor.");
        }
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await GetRefreshTokenAsync();
        if (!string.IsNullOrEmpty(refreshToken))
        {
            try { await http.PostAsJsonAsync("api/auth/logout", new RefreshRequest(refreshToken)); }
            catch { /* igual borramos los tokens locales */ }
        }

        await js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
    }

    // ── Token helpers ─────────────────────────────────────────────────────

    public async Task<string?> GetAccessTokenAsync()
        => await js.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);

    public async Task<string?> GetRefreshTokenAsync()
        => await js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token)) return false;

        // Verificar expiración leyendo el payload del JWT
        try
        {
            var payload  = token.Split('.')[1];
            var padded   = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json     = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var claims   = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (claims is null) return false;

            if (claims.TryGetValue("exp", out var exp))
            {
                var expiry = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64());
                return expiry > DateTimeOffset.UtcNow;
            }
        }
        catch { /* token malformado */ }

        return false;
    }

    public async Task<string?> GetUsernameAsync()
    {
        var token = await GetAccessTokenAsync();
        return token is null ? null : ReadClaim(token, "unique_name") 
                                   ?? ReadClaim(token, "name");
    }

    public async Task<string?> GetRoleAsync()
    {
        var token = await GetAccessTokenAsync();
        return token is null ? null : ReadClaim(token, "role");
    }

    // ── Privados ──────────────────────────────────────────────────────────

    private async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
    }

    private static string? ReadClaim(string token, string claimName)
    {
        try
        {
            var payload = token.Split('.')[1];
            var padded  = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json    = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var claims  = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            return claims?.TryGetValue(claimName, out var val) == true ? val.GetString() : null;
        }
        catch { return null; }
    }

    private record ErrorResponse(string Message);
}