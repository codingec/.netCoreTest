using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace booking.api.test;

/// <summary>
/// Boots the real API (Program.cs) in memory against a throw-away SQLite file.
/// DbInitializerHostedService creates the schema and seed data on startup, exactly as in development.
/// One factory (and therefore one database) is shared by all tests in a class via IClassFixture,
/// so tests within a class must not depend on each other's writes.
/// </summary>
public sealed class BookingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"booking-api-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath};Pooling=False");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch (IOException) { /* best effort */ }
    }
}

internal static class KnownIssue
{
    public const string Prefix = "Known issue (see backend/PROPOSAL.md) — remove Skip once fixed: ";
}

internal static class Seed
{
    public const string AdminEmail = "admin@demo.com";
    public const string Password = "P@ssw0rd!";
}
