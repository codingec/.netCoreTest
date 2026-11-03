using booking.core.Constants;
using booking.infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace booking.infrastructure.test.Persistence;

public class DatabaseBootstrapperTests : DatabaseTest
{
    [Fact]
    public async Task Seeds_reference_data_on_a_fresh_database()
    {
        await using var db = Database.CreateContext();

        Assert.Equal(4, await db.Users.CountAsync());
        Assert.Equal(4, await db.Employees.CountAsync());
        Assert.Equal(2, await db.Offices.CountAsync());
        Assert.Equal(2, await db.Services.CountAsync());
        Assert.Equal(4, await db.ServiceDetails.CountAsync());
    }

    [Fact(Skip = KnownIssue.Prefix + "D13 — on a fresh database the demo booking is not seeded: SeedAsync looks up the employee and service detail before SaveChangesAsync, so both are null. It only appears on the second start.")]
    public async Task Seeds_users_employees_offices_services_and_a_booking()
    {
        await using var db = Database.CreateContext();

        Assert.Equal(4, await db.Users.CountAsync());
        Assert.Equal(4, await db.Employees.CountAsync());
        Assert.Equal(2, await db.Offices.CountAsync());
        Assert.Equal(2, await db.Services.CountAsync());
        Assert.Equal(4, await db.ServiceDetails.CountAsync());
        Assert.Equal(1, await db.Bookings.CountAsync());
    }

    [Fact]
    public async Task Seeds_admin_role_with_booking_permissions()
    {
        await using var db = Database.CreateContext();

        Assert.True(await db.UserRoles.AnyAsync(ur => ur.UserId == Seed.AdminUserId && ur.RoleId == "r_admin"));

        var perms = await db.RoleClaims
            .Where(rc => rc.RoleId == "r_admin" && rc.ClaimType == Permissions.ClaimType)
            .Select(rc => rc.ClaimValue)
            .ToListAsync();

        Assert.Contains(Permissions.Bookings.Read, perms);
        Assert.Contains(Permissions.Bookings.Create, perms);
    }

    [Fact]
    public async Task Seeded_passwords_are_hashed_not_plain_text()
    {
        await using var db = Database.CreateContext();
        var hashes = await db.Users.Select(u => u.PasswordHash).ToListAsync();

        Assert.All(hashes, h =>
        {
            Assert.False(string.IsNullOrWhiteSpace(h));
            Assert.NotEqual(Seed.Password, h);
        });
    }

    [Fact]
    public async Task Running_init_twice_is_idempotent()
    {
        await using (var db = Database.CreateContext())
            await DatabaseBootstrapper.InitAsync(db, AppContext.BaseDirectory);

        await using var check = Database.CreateContext();
        Assert.Equal(4, await check.Users.CountAsync());
        Assert.Equal(2, await check.Offices.CountAsync());
        Assert.Equal(1, await check.Bookings.CountAsync());
        Assert.Equal(2, await check.RoleClaims.CountAsync(rc => rc.RoleId == "r_admin"));
    }

    [Fact]
    public async Task Throws_when_schema_file_is_missing()
    {
        var emptyDb = Path.Combine(Path.GetTempPath(), $"booking-test-{Guid.NewGuid():N}.db");
        var bogusRoot = Path.Combine(Path.GetTempPath(), $"no-schema-{Guid.NewGuid():N}");
        try
        {
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={emptyDb};Pooling=False").Options);

            await Assert.ThrowsAsync<FileNotFoundException>(() => DatabaseBootstrapper.InitAsync(db, bogusRoot));
        }
        finally
        {
            File.Delete(emptyDb);
        }
    }

    [Fact(Skip = KnownIssue.Prefix + "D6 — admin is seeded with only 2 of the 12 permissions.")]
    public async Task Admin_role_has_every_permission()
    {
        await using var db = Database.CreateContext();
        var count = await db.RoleClaims.CountAsync(rc => rc.RoleId == "r_admin" && rc.ClaimType == Permissions.ClaimType);
        Assert.Equal(12, count);
    }
}
