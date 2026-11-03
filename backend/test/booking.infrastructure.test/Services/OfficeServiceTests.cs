using booking.core.Constants;
using booking.core.DTOs;
using booking.infrastructure.Services;

namespace booking.infrastructure.test.Services;

public class OfficeServiceTests : DatabaseTest
{
    [Fact]
    public async Task GetAll_returns_seeded_offices_ordered_by_id()
    {
        await using var db = Database.CreateContext();
        var (ok, data, code) = await new OfficeService(db).GetAllAsync(default);

        Assert.True(ok);
        Assert.Equal(Codes.Ok, code);
        Assert.Equal(new[] { Seed.MainOfficeId, Seed.RemoteOfficeId }, data.Select(o => o.id));
        Assert.Equal("Main Office", data[0].name);
    }

    [Fact]
    public async Task GetById_returns_office()
    {
        await using var db = Database.CreateContext();
        var (ok, data, _) = await new OfficeService(db).GetByIdAsync(Seed.RemoteOfficeId, default);

        Assert.True(ok);
        Assert.Equal("Remote Office", data!.name);
    }

    [Fact]
    public async Task GetById_unknown_returns_not_found()
    {
        await using var db = Database.CreateContext();
        var (ok, data, code) = await new OfficeService(db).GetByIdAsync(999, default);

        Assert.False(ok);
        Assert.Null(data);
        Assert.Equal(Codes.NotFound, code);
    }

    [Fact]
    public async Task Create_persists_office_and_returns_generated_id()
    {
        await using var db = Database.CreateContext();
        var (ok, id, code, _) = await new OfficeService(db).CreateAsync(new OfficeUpsertDto(0, "Branch", "https://img/x.png"), default);

        Assert.True(ok);
        Assert.Equal(Codes.Ok, code);
        Assert.True(id > Seed.RemoteOfficeId);

        await using var check = Database.CreateContext();
        var (_, saved, _) = await new OfficeService(check).GetByIdAsync(id, default);
        Assert.Equal("Branch", saved!.name);
    }

    [Fact]
    public async Task Update_changes_name_and_image()
    {
        await using var db = Database.CreateContext();
        var (ok, code, _) = await new OfficeService(db).UpdateAsync(Seed.MainOfficeId, new OfficeUpsertDto(Seed.MainOfficeId, "HQ", "https://img/hq.png"), default);

        Assert.True(ok);
        Assert.Equal(Codes.Ok, code);

        await using var check = Database.CreateContext();
        var (_, saved, _) = await new OfficeService(check).GetByIdAsync(Seed.MainOfficeId, default);
        Assert.Equal("HQ", saved!.name);
        Assert.Equal("https://img/hq.png", saved.image);
    }

    [Fact]
    public async Task Update_unknown_returns_not_found()
    {
        await using var db = Database.CreateContext();
        var (ok, code, _) = await new OfficeService(db).UpdateAsync(999, new OfficeUpsertDto(999, "x", "y"), default);

        Assert.False(ok);
        Assert.Equal(Codes.NotFound, code);
    }

    [Fact]
    public async Task Delete_removes_office_without_services()
    {
        await using var db = Database.CreateContext();
        var (ok, _, _) = await new OfficeService(db).DeleteAsync(Seed.RemoteOfficeId, default);

        Assert.True(ok);
        await using var check = Database.CreateContext();
        var (found, _, _) = await new OfficeService(check).GetByIdAsync(Seed.RemoteOfficeId, default);
        Assert.False(found);
    }

    [Fact]
    public async Task Delete_unknown_returns_not_found()
    {
        await using var db = Database.CreateContext();
        var (ok, code, _) = await new OfficeService(db).DeleteAsync(999, default);

        Assert.False(ok);
        Assert.Equal(Codes.NotFound, code);
    }

    [Fact(Skip = KnownIssue.Prefix + "D11 — Office.image is NOT NULL in schema.sql but optional in the DTO; null throws DbUpdateException (500).")]
    public async Task Create_without_image_succeeds_or_is_rejected_gracefully()
    {
        await using var db = Database.CreateContext();
        var (ok, _, _, _) = await new OfficeService(db).CreateAsync(new OfficeUpsertDto(0, "No image", null), default);
        Assert.True(ok);
    }

    [Fact(Skip = KnownIssue.Prefix + "D8 — deleting an office that has services throws an FK DbUpdateException (500) instead of returning an error result.")]
    public async Task Delete_office_with_services_returns_error_result_instead_of_throwing()
    {
        await using var db = Database.CreateContext();
        var (ok, _, _) = await new OfficeService(db).DeleteAsync(Seed.MainOfficeId, default);
        Assert.False(ok);
    }
}
