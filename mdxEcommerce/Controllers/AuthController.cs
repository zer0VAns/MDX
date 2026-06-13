using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mdxEcommerce.DTOs;
using mdxEcommerce.Services;

namespace mdxEcommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    // POST api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var (success, error, response) = await authService.RegisterAsync(request);
        if (!success) return BadRequest(new { message = error });
        return Ok(response);
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var (success, error, response) = await authService.LoginAsync(request);
        if (!success) return Unauthorized(new { message = error });
        return Ok(response);
    }

    // POST api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var (success, error, response) = await authService.RefreshAsync(request.RefreshToken);
        if (!success) return Unauthorized(new { message = error });
        return Ok(response);
    }

    // POST api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        await authService.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    // GET api/auth/me  — para que el cliente verifique su sesión actual
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        var email    = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role     = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new { userId, email, username, role });
    }
}