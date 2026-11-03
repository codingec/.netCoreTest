using booking.core.Constants;
using booking.core.DTOs;
using booking.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace booking.infrastructure.test.Services;

public class ServicesServiceTests : DatabaseTest
{
    private static ServiceUpsertDto NewService(string name, int officeId, params string[] detailNames) =>
        new(name, officeId, "desc", 3, 5, 1, "15-60", "https://img/s.png",
            detailNames.Select(n => new ServiceDetailUpsertDto(null, n, $"{n} desc", 2, "https://img/i.png")).ToList());

    [Fact]
    public async Task GetAll_returns_services_with_their_details()
    {
        await using var db = Database.CreateContext();
        var (ok, data, _) = await new ServicesService(db).GetAllAsync(default);

        Assert.True(ok);
        Assert.Equal(2, data.Count);
        var rooms = data.Single(s => s.id == Seed.RoomBookingServiceId);
        Assert.Equal("Room Booking", rooms.name);
        Assert.Equal(new[] { "Small Room", "Large Room" }, rooms.serviceDetails.Select(d => d.name));
    }

    [Fact]
    public async Task GetById_returns_details_ordered_by_id()
    {
        await using var db = Database.CreateContext();
        var (ok, data, _) = await new ServicesService(db).GetByIdAsync(Seed.EquipmentServiceId, default);

        Assert.True(ok);
        Assert.Equal("Equipment Rental", data!.name);
        Assert.Equal(data.serviceDetails.Select(d => d.id).OrderBy(i => i), data.serviceDetails.Select(d => d.id));
        Assert.All(data.serviceDetails, d => Assert.Equal(Seed.EquipmentServiceId, d.serviceId));
    }

    [Fact]
    public async Task GetById_unknown_returns_not_found()
    {
        await using var db = Database.CreateContext();
        var (ok, data, code) = await new ServicesService(db).GetByIdAsync(999, default);

        Assert.False(ok);
        Assert.Null(data);
        Assert.Equal(Codes.NotFound, code);
    }

    [Fact]
    public async Task Create_persists_service_with_details_and_timestamps()
    {
        await using var db = Database.CreateContext();
        var (ok, id, code, _) = await new ServicesService(db).CreateAsync(NewService("Parking", Seed.MainOfficeId, "Spot A", "Spot B"), default);

        Assert.True(ok);
        Assert.Equal(Codes.Ok, code);

        await using var check = Database.CreateContext();
        var saved = await check.Services.Include(s => s.ServiceDetails).SingleAsync(s => s.Id == id);
        Assert.Equal("Parking", saved.Name);
        Assert.Equal("15-60", saved.MinMax);
        Assert.NotNull(saved.CreatedAt);
        Assert.Equal(2, saved.ServiceDetails.Count);
        Assert.All(saved.ServiceDetails, d => Assert.NotNull(d.CreatedAt));
    }

    [Fact]
    public async Task Create_without_details_is_allowed()
    {
        await using var db = Database.CreateContext();
        var (ok, id, _, _) = await new ServicesService(db).CreateAsync(NewService("Lockers", Seed.MainOfficeId), default);

        Assert.True(ok);
        await using var check = Database.CreateContext();
        Assert.Equal(0, await check.ServiceDetails.CountAsync(d => d.ServiceId == id));
    }

    [Fact]
    public async Task Update_changes_fields_and_replaces_details()
    {
        await using var db = Database.CreateContext();
        var dto = NewService("Equipment v2", Seed.MainOfficeId, "Monitor");
        var (ok, code, _) = await new ServicesService(db).UpdateAsync(Seed.EquipmentServiceId, dto, default);

        Assert.True(ok);
        Assert.Equal(Codes.Ok, code);

        await using var check = Database.CreateContext();
        var saved = await check.Services.Include(s => s.ServiceDetails).SingleAsync(s => s.Id == Seed.EquipmentServiceId);
        Assert.Equal("Equipment v2", saved.Name);
        Assert.Equal(new[] { "Monitor" }, saved.ServiceDetails.Select(d => d.Name));
    }

    [Fact]
    public async Task Update_unknown_returns_not_found()
    {
        await using var db = Database.CreateContext();
        var (ok, code, _) = await new ServicesService(db).UpdateAsync(999, NewService("x", Seed.MainOfficeId), default);

        Assert.False(ok);
        Assert.Equal(Codes.NotFound, code);
    }

    [Fact]
    public async Task Delete_removes_service_without_details()
    {
        await using var db = Database.CreateContext();
        var (_, id, _, _) = await new ServicesService(db).CreateAsync(NewService("Temp", Seed.MainOfficeId), default);

        await using var db2 = Database.CreateContext();
        var (ok, _, _) = await new ServicesService(db2).DeleteAsync(id, default);

        Assert.True(ok);
        await using var check = Database.CreateContext();
        Assert.False(await check.Services.AnyAsync(s => s.Id == id));
    }

    [Fact]
    public async Task Delete_unknown_returns_not_found()
    {
        await using var db = Database.CreateContext();
        var (ok, code, _) = await new ServicesService(db).DeleteAsync(999, default);

        Assert.False(ok);
        Assert.Equal(Codes.NotFound, code);
    }

    [Fact(Skip = KnownIssue.Prefix + "D7 — UpdateAsync deletes and recreates all details, so detail ids change on every edit.")]
    public async Task Update_keeps_ids_of_existing_details()
    {
        // Uses service 1 (details 1 and 2): SQLite reuses max(rowid)+1, so for the *last* ids (3, 4)
        // a delete-and-recreate would get the same ids back by coincidence.
        await using var db = Database.CreateContext();
        var dto = new ServiceUpsertDto("Room Booking", Seed.MainOfficeId, null, 10, 5, 2, "15-45", null,
            new List<ServiceDetailUpsertDto>
            {
                new(Seed.SmallRoomDetailId, "Small Room (renamed)", null, 4, null),
                new(Seed.LargeRoomDetailId, "Large Room", null, 10, null),
            });

        await new ServicesService(db).UpdateAsync(Seed.RoomBookingServiceId, dto, default);

        await using var check = Database.CreateContext();
        var details = await check.ServiceDetails.Where(d => d.ServiceId == Seed.RoomBookingServiceId).OrderBy(d => d.Id).ToListAsync();
        Assert.Equal(new[] { Seed.SmallRoomDetailId, Seed.LargeRoomDetailId }, details.Select(d => d.Id));
        Assert.Equal("Small Room (renamed)", details[0].Name);
    }

    [Fact(Skip = KnownIssue.Prefix + "D7 — updating a service whose details have bookings throws an FK DbUpdateException (500).")]
    public async Task Update_service_whose_details_have_bookings_succeeds()
    {
        await using (var seedDb = Database.CreateContext())
        {
            var start = new DateTime(2030, 1, 15, 9, 0, 0);
            var (created, _, _, _) = await new BookingService(seedDb).CreateAsync(
                new BookingUpsertDto(0, Seed.SmallRoomDetailId, Seed.SarahEmployeeId, start, start.AddHours(1), start), default);
            Assert.True(created);
        }

        await using var db = Database.CreateContext();
        var dto = NewService("Room Booking", Seed.MainOfficeId, "Small Room", "Large Room");

        var (ok, _, _) = await new ServicesService(db).UpdateAsync(Seed.RoomBookingServiceId, dto, default);

        Assert.True(ok);
    }

    [Fact(Skip = KnownIssue.Prefix + "D8 — deleting a service with details throws an FK DbUpdateException (500); schema.sql has no ON DELETE behaviour.")]
    public async Task Delete_service_with_details_returns_result_instead_of_throwing()
    {
        await using var db = Database.CreateContext();
        var (ok, _, _) = await new ServicesService(db).DeleteAsync(Seed.EquipmentServiceId, default);
        Assert.True(ok);
    }
}
