using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using booking.core.Constants;
using booking.core.Models;

namespace booking.infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task EnsureCreatedAndSeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@demo.com", ct);

        if (user is null)
        {
            user = new User
            {
                UserName = "admin",
                Email = "admin@demo.com",
                EmailConfirmed = true
            };

            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, "P@ssw0rd!");

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }

        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == "r_admin", ct);
        if (role is null)
        {
            role = new Role { Id = "r_admin", Name = "Admin" };
            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);
        }

        var hasUserRole = await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == "r_admin", ct);
        if (!hasUserRole)
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = "r_admin" });

        var needRead = !await db.RoleClaims.AnyAsync(rc =>
            rc.RoleId == "r_admin" && rc.ClaimType == Permissions.ClaimType && rc.ClaimValue == Permissions.Bookings.Read, ct);

        var needCreate = !await db.RoleClaims.AnyAsync(rc =>
            rc.RoleId == "r_admin" && rc.ClaimType == Permissions.ClaimType && rc.ClaimValue == Permissions.Bookings.Create, ct);

        if (needRead)
            db.RoleClaims.Add(new RoleClaim { RoleId = "r_admin", ClaimType = Permissions.ClaimType, ClaimValue = Permissions.Bookings.Read });

        if (needCreate)
            db.RoleClaims.Add(new RoleClaim { RoleId = "r_admin", ClaimType = Permissions.ClaimType, ClaimValue = Permissions.Bookings.Create });

        await db.SaveChangesAsync(ct);
    }
}
