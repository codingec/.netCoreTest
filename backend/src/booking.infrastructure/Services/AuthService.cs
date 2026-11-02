using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using booking.core.Constants;
using booking.core.DTOs;
using booking.core.Models;
using booking.infrastructure.Persistence;

namespace booking.infrastructure.Services;

public interface IAuthService
{
    Task<(bool ok, AuthResponse? data, string? code)> LoginAsync(string email, string password, CancellationToken ct);
    Task<(bool ok, AuthResponse? data, string code, string? message)> RegisterAsync(string email, string password, string? userName, CancellationToken ct);

}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(bool ok, AuthResponse? data, string? code)> LoginAsync(
        string email, string password, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower(), ct);

        if (user is null)
            return (false, null, Codes.UserNotFound);

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return (false, null, Codes.InvalidCredentials);

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash!, password);
        if (verify == PasswordVerificationResult.Failed)
            return (false, null, Codes.InvalidCredentials);

        var roles = user.Roles.Select(r => r.Role.Name!).Where(n => n != null).Distinct().ToArray();

        var userPermissions = await _db.UserClaims
            .Where(c => c.UserId == user.Id && c.ClaimType == Permissions.ClaimType)
            .Select(c => c.ClaimValue!)
            .ToListAsync(ct);

        var rolePermissions = await _db.RoleClaims
            .Where(rc => user.Roles.Select(r => r.RoleId).Contains(rc.RoleId) && rc.ClaimType == Permissions.ClaimType)
            .Select(rc => rc.ClaimValue!)
            .ToListAsync(ct);

        var permissions = userPermissions.Concat(rolePermissions).Where(p => p != null).Distinct().ToArray();

        var employeeId = await _db.Employees
            .Where(e => e.UserId == user.Id)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(ct);

        var (token, exp) = CreateJwt(user, roles, permissions, employeeId);

        var resp = new AuthResponse(token, exp, user.Id, user.UserName, roles, permissions, employeeId);
        return (true, resp, Codes.Ok);
    }

    public async Task<(bool ok, AuthResponse? data, string code, string? message)> RegisterAsync(
        string email, string password, string? userName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, null, Codes.EmailRequired, "Email is required.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8
            || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
        {
            return (false, null, Codes.PasswordWeak, "Password must be 8+ chars with upper, lower and digit.");
        }

        var exists = await _db.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower(), ct);
        if (exists)
            return (false, null, Codes.EmailAlreadyExists, "Email already registered.");

        var user = new User
        {
            Email = email,
            UserName = string.IsNullOrWhiteSpace(userName) ? email : userName,
            EmailConfirmed = true
        };
        user.PasswordHash = _hasher.HashPassword(user, password);

        const string defaultRoleId = "r_user";
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == defaultRoleId, ct);
        if (role is null)
        {
            role = new Role { Id = defaultRoleId, Name = "User" };
            _db.Roles.Add(role);
        }

        _db.Users.Add(user);
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = defaultRoleId });

        await _db.SaveChangesAsync(ct);

        var roles = new[] { role.Name ?? "User" };

        var rolePerms = await _db.RoleClaims
            .Where(rc => rc.RoleId == defaultRoleId && rc.ClaimType == Permissions.ClaimType)
            .Select(rc => rc.ClaimValue!)
            .ToListAsync(ct);

        var perms = rolePerms.Distinct().ToArray();

        var employeeId = await _db.Employees
            .Where(e => e.UserId == user.Id)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(ct);

        var (token, exp) = CreateJwt(user, roles, perms, employeeId);
        var resp = new AuthResponse(token, exp, user.Id, user.UserName, roles, perms, employeeId);

        return (true, resp, Codes.Ok, null);
    }

    private (string token, DateTime expiresAt) CreateJwt(User user, IEnumerable<string> roles, IEnumerable<string> perms, int? employeeId)
    {
        var jwtSection = _config.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(perms.Select(p => new Claim(Permissions.ClaimType, p)));

        if (employeeId.HasValue)
        {
            claims.Add(new Claim("employee_id", employeeId.Value.ToString()));
        }

        var expires = DateTime.UtcNow.AddHours(2);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}