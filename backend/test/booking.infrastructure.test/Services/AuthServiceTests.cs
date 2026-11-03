using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using booking.core.Constants;
using booking.infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace booking.infrastructure.test.Services;

public class AuthServiceTests : DatabaseTest
{
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";

    private static IConfiguration Config() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:Key"] = new string('k', 64),
            })
            .Build();

    private AuthService CreateSut(out booking.infrastructure.Persistence.AppDbContext db)
    {
        db = Database.CreateContext();
        return new AuthService(db, Config());
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_with_valid_credentials_returns_token_and_profile()
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (ok, data, code) = await sut.LoginAsync(Seed.AdminEmail, Seed.Password, default);

        Assert.True(ok);
        Assert.Equal(Codes.Ok, code);
        Assert.NotNull(data);
        Assert.Equal(Seed.AdminUserId, data!.UserId);
        Assert.Equal(Seed.AdminEmployeeId, data.employeeId);
        Assert.Contains("Admin", data.Roles);
        Assert.Contains(Permissions.Bookings.Read, data.Permissions);
        Assert.InRange(data.ExpiresAt, DateTime.UtcNow.AddHours(1.9), DateTime.UtcNow.AddHours(2.1));
    }

    [Fact]
    public async Task Login_token_carries_identity_role_permission_and_employee_claims()
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (_, data, _) = await sut.LoginAsync(Seed.AdminEmail, Seed.Password, default);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(data!.Token);

        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);
        Assert.Equal(Seed.AdminUserId.ToString(), jwt.Subject);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type == Permissions.ClaimType && c.Value == Permissions.Bookings.Create);
        Assert.Contains(jwt.Claims, c => c.Type == "employee_id" && c.Value == Seed.AdminEmployeeId.ToString());
    }

    [Fact]
    public async Task Login_email_is_case_insensitive()
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (ok, _, _) = await sut.LoginAsync("ADMIN@Demo.com", Seed.Password, default);

        Assert.True(ok);
    }

    [Fact]
    public async Task Login_with_wrong_password_fails_with_invalid_credentials()
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (ok, data, code) = await sut.LoginAsync(Seed.AdminEmail, "wrong-password", default);

        Assert.False(ok);
        Assert.Null(data);
        Assert.Equal(Codes.InvalidCredentials, code);
    }

    [Fact]
    public async Task Login_with_unknown_email_fails()
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (ok, data, _) = await sut.LoginAsync("nobody@demo.com", Seed.Password, default);

        Assert.False(ok);
        Assert.Null(data);
    }

    [Fact(Skip = KnownIssue.Prefix + "S5 — unknown email returns ERR_AUTH_USER_NOT_FOUND, which enables user enumeration.")]
    public async Task Login_unknown_email_and_wrong_password_are_indistinguishable()
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (_, _, unknownCode) = await sut.LoginAsync("nobody@demo.com", Seed.Password, default);
        var (_, _, wrongPwdCode) = await sut.LoginAsync(Seed.AdminEmail, "wrong-password", default);

        Assert.Equal(wrongPwdCode, unknownCode);
    }

    // ---------- Register ----------

    [Fact]
    public async Task Register_creates_user_with_default_role_and_returns_token()
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (ok, data, code, message) = await sut.RegisterAsync("new.user@demo.com", "Passw0rd", "newuser", default);

        Assert.True(ok, message);
        Assert.Equal(Codes.Ok, code);
        Assert.NotNull(data);
        Assert.True(data!.UserId > 0);
        Assert.Equal("newuser", data.UserName);
        Assert.Contains("User", data.Roles);
        Assert.False(string.IsNullOrWhiteSpace(data.Token));
    }

    [Fact]
    public async Task Register_links_user_role_to_the_generated_user_id()
    {
        // Regression: UserRole used to be created with UserId = 0 (before the id was generated),
        // which failed the FK on SaveChanges.
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (_, data, _, _) = await sut.RegisterAsync("role.link@demo.com", "Passw0rd", null, default);

        await using var check = Database.CreateContext();
        var link = await check.UserRoles.SingleAsync(ur => ur.UserId == data!.UserId);
        Assert.Equal("r_user", link.RoleId);
        Assert.False(await check.UserRoles.AnyAsync(ur => ur.UserId == 0));
    }

    [Fact]
    public async Task Registered_user_can_log_in()
    {
        var sut = CreateSut(out var db);
        await using var _ = db;
        await sut.RegisterAsync("login.after@demo.com", "Passw0rd", null, default);

        await using var db2 = Database.CreateContext();
        var (ok, data, _) = await new AuthService(db2, Config()).LoginAsync("login.after@demo.com", "Passw0rd", default);

        Assert.True(ok);
        Assert.Equal("login.after@demo.com", data!.UserName); // username defaults to email
    }

    [Fact]
    public async Task Second_registration_reuses_existing_default_role()
    {
        var sut = CreateSut(out var db);
        await using var _ = db;
        await sut.RegisterAsync("first@demo.com", "Passw0rd", null, default);

        await using var db2 = Database.CreateContext();
        var (ok, _, _, message) = await new AuthService(db2, Config()).RegisterAsync("second@demo.com", "Passw0rd", null, default);

        Assert.True(ok, message);
        await using var check = Database.CreateContext();
        Assert.Equal(1, await check.Roles.CountAsync(r => r.Id == "r_user"));
    }

    [Theory]
    [InlineData("ADMIN@demo.com")]
    [InlineData("admin@demo.com")]
    public async Task Register_rejects_existing_email_regardless_of_case(string email)
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (ok, data, code, _) = await sut.RegisterAsync(email, "Passw0rd", null, default);

        Assert.False(ok);
        Assert.Null(data);
        Assert.Equal(Codes.EmailAlreadyExists, code);
    }

    [Theory]
    [InlineData("short1A")]      // too short
    [InlineData("alllower1")]    // no upper
    [InlineData("ALLUPPER1")]    // no lower
    [InlineData("NoDigitsHere")] // no digit
    [InlineData("")]
    public async Task Register_rejects_weak_passwords(string password)
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (ok, _, code, _) = await sut.RegisterAsync("weak@demo.com", password, null, default);

        Assert.False(ok);
        Assert.Equal(Codes.PasswordWeak, code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_requires_email(string email)
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (ok, _, code, _) = await sut.RegisterAsync(email, "Passw0rd", null, default);

        Assert.False(ok);
        Assert.Equal(Codes.EmailRequired, code);
    }

    [Fact(Skip = KnownIssue.Prefix + "D5 — role r_user has no permissions, so registered users cannot book.")]
    public async Task Registered_user_has_booking_permissions()
    {
        var sut = CreateSut(out var db);
        await using var _ = db;

        var (_, data, _, _) = await sut.RegisterAsync("perm@demo.com", "Passw0rd", null, default);

        Assert.Contains(Permissions.Bookings.Create, data!.Permissions);
    }
}
