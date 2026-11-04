using booking.infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace booking.infrastructure.test;

/// <summary>
/// A throw-away SQLite file database, created with the real schema.sql and seed
/// (DatabaseBootstrapper), so tests see the same constraints as the app, including foreign keys.
/// EF InMemory is deliberately not used: it ignores FKs, NOT NULL and transactions.
/// A file is used instead of ":memory:" because DatabaseBootstrapper disposes the EF connection,
/// which would drop an in-memory database.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private readonly string _path;

    private TestDatabase(string path)
    {
        _path = path;
        ConnectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();
    }

    public string ConnectionString { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var db = new TestDatabase(Path.Combine(Path.GetTempPath(), $"booking-test-{Guid.NewGuid():N}.db"));
        await using var ctx = db.CreateContext();
        // schema.sql is copied to <output>/Persistence/Sql/schema.sql via the project reference.
        await DatabaseBootstrapper.InitAsync(ctx, AppContext.BaseDirectory);
        return db;
    }

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(ConnectionString).Options);

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_path); } catch (IOException) { /* best effort */ }
        return ValueTask.CompletedTask;
    }
}

/// <summary>Base class: a fresh seeded database per test (xUnit creates one class instance per test).</summary>
public abstract class DatabaseTest : IAsyncLifetime
{
    protected TestDatabase Database { get; private set; } = default!;

    public async Task InitializeAsync() => Database = await TestDatabase.CreateAsync();

    public async Task DisposeAsync() => await Database.DisposeAsync();

    /// <summary>Known seed data (see DatabaseBootstrapper.SeedAsync).</summary>
    protected static class Seed
    {
        public const string AdminEmail = "admin@demo.com";
        public const string Password = "P@ssw0rd!";
        public const int AdminUserId = 1;
        public const int AdminEmployeeId = 101;
        public const int SarahEmployeeId = 102;
        public const int MainOfficeId = 1;
        public const int RemoteOfficeId = 2;
        public const int RoomBookingServiceId = 1;     // has the seeded booking
        public const int EquipmentServiceId = 2;       // has details, no bookings
        public const int SmallRoomDetailId = 1;
        public const int LargeRoomDetailId = 2;
        public const int ProjectorDetailId = 3;
        public const int LaptopDetailId = 4;
    }

    protected static class KnownIssue
    {
        public const string Prefix = "Known issue (see backend/PROPOSAL.md) — remove Skip once fixed: ";
    }
}
