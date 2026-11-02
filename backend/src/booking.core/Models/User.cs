namespace booking.core.Models;

public class User
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string? PhoneNumber { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime? CreatedAt { get; set; }

    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
    public ICollection<UserClaim> Claims { get; set; } = new List<UserClaim>();
}

public class UserRole
{
    public int UserId { get; set; }
    public string RoleId { get; set; } = default!;
    public User User { get; set; } = default!;
    public Role Role { get; set; } = default!;
}

public class UserClaim
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? ClaimType { get; set; }
    public string? ClaimValue { get; set; }
}