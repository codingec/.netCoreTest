namespace booking.core.DTOs;

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    int UserId,
    string? UserName,
    IEnumerable<string> Roles,
    IEnumerable<string> Permissions,
    int? employeeId
);

public record RegisterRequest(
    string Email,
    string Password,
    string? UserName
);