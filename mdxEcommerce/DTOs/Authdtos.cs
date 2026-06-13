namespace mdxEcommerce.DTOs;

public record RegisterRequest(
    string Email,
    string Username,
    string Password,
    string? FirstName,
    string? LastName
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string Email,
    string Username,
    string Role
);

public record RefreshRequest(
    string RefreshToken
);