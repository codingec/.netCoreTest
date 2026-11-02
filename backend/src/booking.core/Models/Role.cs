namespace booking.core.Models;

public class Role
{
    public string Id { get; set; } = default!;
    public string? Name { get; set; }

    public ICollection<UserRole> Users { get; set; } = new List<UserRole>();
    public ICollection<RoleClaim> Claims { get; set; } = new List<RoleClaim>();
}

public class RoleClaim
{
    public int Id { get; set; }
    public string RoleId { get; set; } = default!;
    public string? ClaimType { get; set; }
    public string? ClaimValue { get; set; }
}