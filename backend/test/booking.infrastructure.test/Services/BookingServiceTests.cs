using booking.core.Constants;
using booking.core.DTOs;
using booking.infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace booking.infrastructure.test.Services;

public class BookingServiceTests : DatabaseTest
{
    // A date far from the seeded booking, with Unspecified kind (as the API receives it).
    private static readonly DateTime Day = new(2030, 1, 15);

    private static BookingUpsertDto NewBooking(int detailId, int employeeId, string start, string end) =>
        new(0, detailId, employeeId, Day.Add(TimeSpan.Parse(start)), Day.Add(TimeSpan.Parse(end)), new DateTime(2030, 1, 1));

    private async Task<int> CreateAsync(BookingUpsertDto dto)
    {
        await using var db = Database.CreateContext();
        var (ok, id, _, message) = await new BookingService(db).CreateAsync(dto, default);
        Assert.True(ok, message);
        return id;
    }

    private async Task<List<AvailabilityTimes>> AvailabilityAsync(int detailId, int frequency)
    {
        await using var db = Database.CreateContext();
        var (ok, data, _) = await new BookingService(db).GetAvailability(
            new Availability(detailId, Day.ToString("yyyy-MM-dd"), frequency.ToString()), default);
        Assert.True(ok);
        return data;
    }

    // ---------- Queries ----------

    [Fact]
    public async Task GetAll_returns_bookings_ordered_by_id()
    {
        var first = await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "09:00", "10:00"));
        var second = await CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "09:00", "10:00"));

        await using var db = Database.CreateContext();
        var (ok, data, code) = await new BookingService(db).GetAllAsync(default);

        Assert.True(ok);
        Assert.Equal(Codes.Ok, code);
        var ids = data.Select(b => b.id).ToList();
        Assert.Equal(ids.OrderBy(i => i), ids);
        Assert.Contains(first, ids);
        Assert.Contains(second, ids);
        Assert.Equal(Seed.SarahEmployeeId, data.Single(b => b.id == first).employeeId);
        Assert.Equal(Seed.SmallRoomDetailId, data.Single(b => b.id == first).serviceDetailId);
    }

    [Fact]
    public async Task GetById_returns_booking()
    {
        var id = await CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "09:00", "10:00"));

        await using var db = Database.CreateContext();
        var (ok, data, _) = await new BookingService(db).GetByIdAsync(id, default);

        Assert.True(ok);
        Assert.Equal(Seed.LargeRoomDetailId, data!.serviceDetailId);
        Assert.Equal(Day.AddHours(9), data.startDatetime);
        Assert.Equal(Day.AddHours(10), data.endDatetime);
    }

    [Fact]
    public async Task GetById_unknown_returns_not_found()
    {
        await using var db = Database.CreateContext();
        var (ok, data, code) = await new BookingService(db).GetByIdAsync(999, default);

        Assert.False(ok);
        Assert.Null(data);
        Assert.Equal(Codes.NotFound, code);
    }

    [Fact]
    public async Task GetByServiceId_returns_rows_with_employee_and_detail_names()
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "09:00", "10:00"));

        await using var db = Database.CreateContext();
        var (ok, data, _) = await new BookingService(db).GetByServiceIdAsync(Seed.RoomBookingServiceId, default);

        Assert.True(ok);
        var row = Assert.Single(data);
        Assert.Equal("Sarah Johnson", row.EmployeeName);
        Assert.Equal("Small Room", row.ServiceDetailName);
    }

    [Fact]
    public async Task GetByServiceId_for_service_without_bookings_returns_empty_list()
    {
        await using var db = Database.CreateContext();
        var (ok, data, _) = await new BookingService(db).GetByServiceIdAsync(Seed.EquipmentServiceId, default);

        Assert.True(ok);
        Assert.Empty(data);
    }

    // ---------- Commands ----------

    [Fact]
    public async Task Create_persists_booking()
    {
        var id = await CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "11:00", "11:30"));

        await using var check = Database.CreateContext();
        var saved = await check.Bookings.SingleAsync(b => b.Id == id);
        Assert.Equal(Seed.AdminEmployeeId, saved.EmployeeId);
        Assert.Equal(Day.AddHours(11), saved.StartDatetime);
    }

    [Fact]
    public async Task Update_moves_booking()
    {
        var id = await CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "11:00", "11:30"));

        await using var db = Database.CreateContext();
        var (ok, code, _) = await new BookingService(db).UpdateAsync(id, NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "13:00", "13:30"), default);

        Assert.True(ok);
        Assert.Equal(Codes.Ok, code);
        await using var check = Database.CreateContext();
        Assert.Equal(Day.AddHours(13), (await check.Bookings.SingleAsync(b => b.Id == id)).StartDatetime);
    }

    [Fact]
    public async Task Update_unknown_returns_not_found()
    {
        await using var db = Database.CreateContext();
        var (ok, code, _) = await new BookingService(db).UpdateAsync(999, NewBooking(1, Seed.AdminEmployeeId, "09:00", "10:00"), default);

        Assert.False(ok);
        Assert.Equal(Codes.NotFound, code);
    }

    [Fact]
    public async Task Delete_removes_booking()
    {
        var id = await CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "11:00", "11:30"));

        await using var db = Database.CreateContext();
        var (ok, _, _) = await new BookingService(db).DeleteAsync(id, default);

        Assert.True(ok);
        await using var check = Database.CreateContext();
        Assert.False(await check.Bookings.AnyAsync(b => b.Id == id));
    }

    [Fact]
    public async Task Delete_unknown_returns_not_found()
    {
        await using var db = Database.CreateContext();
        var (ok, code, _) = await new BookingService(db).DeleteAsync(999, default);

        Assert.False(ok);
        Assert.Equal(Codes.NotFound, code);
    }

    private const string ConflictMessage = "This time is already booked. Please choose another time.";

    [Fact]
    public async Task Create_rejects_overlapping_booking_for_same_resource()
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.AdminEmployeeId, "10:00", "11:00"));

        await using var db = Database.CreateContext();
        var before = await db.Bookings.CountAsync(b => b.ServiceDetailId == Seed.SmallRoomDetailId);
        var (ok, id, code, message) = await new BookingService(db).CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:30", "11:30"), default);

        Assert.False(ok);
        Assert.Equal(0, id);
        Assert.Equal(Codes.BookingConflict, code);
        Assert.Equal(ConflictMessage, message);

        await using var check = Database.CreateContext();
        Assert.Equal(before, await check.Bookings.CountAsync(b => b.ServiceDetailId == Seed.SmallRoomDetailId));
    }

    [Theory]
    [InlineData("10:00", "11:00")] // same range
    [InlineData("09:00", "12:00")] // covers the existing booking
    [InlineData("10:15", "10:45")] // inside the existing booking
    [InlineData("09:30", "10:30")] // overlaps its start
    public async Task Create_rejects_any_overlap_for_same_resource(string start, string end)
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00"));

        await using var db = Database.CreateContext();
        var (ok, _, code, _) = await new BookingService(db).CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.AdminEmployeeId, start, end), default);

        Assert.False(ok);
        Assert.Equal(Codes.BookingConflict, code);
    }

    [Fact]
    public async Task Create_rejects_overlap_by_the_same_employee()
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00"));

        await using var db = Database.CreateContext();
        var (ok, _, code, message) = await new BookingService(db).CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:30", "11:30"), default);

        Assert.False(ok);
        Assert.Equal(Codes.BookingConflict, code);
        Assert.Equal(ConflictMessage, message);
    }

    [Theory]
    [InlineData("10:00", "11:00")] // same range
    [InlineData("10:30", "11:30")] // partial overlap
    public async Task Create_allows_same_employee_at_the_same_time_in_a_different_room(string start, string end)
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00"));

        await using var db = Database.CreateContext();
        var (ok, id, _, message) = await new BookingService(db).CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.SarahEmployeeId, start, end), default);

        Assert.True(ok, message);
        Assert.True(id > 0);
    }

    [Theory]
    [InlineData(Seed.ProjectorDetailId)]
    [InlineData(Seed.LaptopDetailId)]
    public async Task Create_allows_other_employees_to_share_equipment_at_the_same_time(int detailId)
    {
        await CreateAsync(NewBooking(detailId, Seed.SarahEmployeeId, "10:00", "11:00"));

        await using var db = Database.CreateContext();
        var (ok, id, _, message) = await new BookingService(db).CreateAsync(NewBooking(detailId, Seed.AdminEmployeeId, "10:00", "11:00"), default);

        Assert.True(ok, message);
        Assert.True(id > 0);
    }

    [Theory]
    [InlineData(Seed.ProjectorDetailId)]
    [InlineData(Seed.LaptopDetailId)]
    public async Task Create_rejects_same_employee_repeating_equipment_at_the_same_time(int detailId)
    {
        await CreateAsync(NewBooking(detailId, Seed.SarahEmployeeId, "10:00", "11:00"));

        await using var db = Database.CreateContext();
        var (ok, _, code, message) = await new BookingService(db).CreateAsync(NewBooking(detailId, Seed.SarahEmployeeId, "10:30", "11:30"), default);

        Assert.False(ok);
        Assert.Equal(Codes.BookingConflict, code);
        Assert.Equal(ConflictMessage, message);
    }

    [Fact]
    public async Task Create_allows_same_employee_back_to_back_in_a_different_room()
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00"));

        await using var db = Database.CreateContext();
        var (ok, _, _, message) = await new BookingService(db).CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.SarahEmployeeId, "11:00", "12:00"), default);

        Assert.True(ok, message);
    }

    [Theory]
    [InlineData("11:00", "11:30")] // starts when the existing one ends
    [InlineData("09:30", "10:00")] // ends when the existing one starts
    public async Task Create_allows_back_to_back_bookings(string start, string end)
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00"));

        await using var db = Database.CreateContext();
        var (ok, id, _, message) = await new BookingService(db).CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.AdminEmployeeId, start, end), default);

        Assert.True(ok, message);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task Create_allows_another_employee_at_the_same_time_on_a_different_resource()
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00"));

        await using var db = Database.CreateContext();
        var (ok, id, _, message) = await new BookingService(db).CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "10:00", "11:00"), default);

        Assert.True(ok, message);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task Create_allows_only_one_of_several_concurrent_requests_for_the_same_slot()
    {
        // The race window is small, so try many slots, each with requests released at the same moment.
        const int rounds = 5, requests = 8;

        for (var round = 0; round < rounds; round++)
        {
            var dto = NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00") with
            {
                startDatetime = Day.AddDays(round).AddHours(10),
                endDatetime = Day.AddDays(round).AddHours(11),
            };
            using var start = new Barrier(requests);

            var results = await Task.WhenAll(Enumerable.Range(0, requests).Select(_ => Task.Run(async () =>
            {
                await using var db = Database.CreateContext();
                await db.Bookings.AnyAsync(); // open the connection before the requests are released
                start.SignalAndWait();
                return await new BookingService(db).CreateAsync(dto, default);
            })));

            Assert.Single(results, r => r.ok);
            Assert.All(results.Where(r => !r.ok), r => Assert.Equal(Codes.BookingConflict, r.code));
        }
    }

    [Fact]
    public async Task Update_rejects_moving_onto_a_taken_room_and_keeps_the_booking()
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00"));
        var id = await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.AdminEmployeeId, "13:00", "14:00"));

        await using var db = Database.CreateContext();
        var (ok, code, message) = await new BookingService(db).UpdateAsync(id, NewBooking(Seed.SmallRoomDetailId, Seed.AdminEmployeeId, "10:30", "11:30"), default);

        Assert.False(ok);
        Assert.Equal(Codes.BookingConflict, code);
        Assert.Equal(ConflictMessage, message);
        await using var check = Database.CreateContext();
        Assert.Equal(Day.AddHours(13), (await check.Bookings.SingleAsync(b => b.Id == id)).StartDatetime);
    }

    [Fact]
    public async Task Update_allows_overlapping_the_same_employees_booking_in_a_different_room()
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00"));
        var id = await CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.SarahEmployeeId, "13:00", "14:00"));

        await using var db = Database.CreateContext();
        var (ok, _, message) = await new BookingService(db).UpdateAsync(id, NewBooking(Seed.LargeRoomDetailId, Seed.SarahEmployeeId, "10:30", "11:30"), default);

        Assert.True(ok, message);
    }

    [Fact]
    public async Task Update_allows_overlapping_its_own_previous_time()
    {
        var id = await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00"));

        await using var db = Database.CreateContext();
        var (ok, _, message) = await new BookingService(db).UpdateAsync(id, NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:30", "11:30"), default);

        Assert.True(ok, message);
    }

    [Fact]
    public async Task Update_allows_moving_to_a_different_room_at_the_same_time()
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.SarahEmployeeId, "10:00", "11:00"));
        var id = await CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "13:00", "14:00"));

        await using var db = Database.CreateContext();
        var (ok, _, message) = await new BookingService(db).UpdateAsync(id, NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "10:00", "11:00"), default);

        Assert.True(ok, message);
    }

    [Fact(Skip = KnownIssue.Prefix + "D4 — no validation that start is before end.")]
    public async Task Create_rejects_end_before_start()
    {
        await using var db = Database.CreateContext();
        var (ok, _, _, _) = await new BookingService(db).CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.AdminEmployeeId, "11:00", "10:00"), default);

        Assert.False(ok);
    }

    [Fact(Skip = KnownIssue.Prefix + "A1/D9 — unknown employee or detail throws an FK DbUpdateException (500) instead of an error result.")]
    public async Task Create_with_unknown_employee_returns_error_result()
    {
        await using var db = Database.CreateContext();
        var (ok, _, _, _) = await new BookingService(db).CreateAsync(NewBooking(Seed.SmallRoomDetailId, 9999, "09:00", "10:00"), default);

        Assert.False(ok);
    }

    // ---------- Availability ----------

    [Theory]
    [InlineData(60, 7)]   // 08:00 .. 14:00
    [InlineData(30, 14)]  // 08:00 .. 14:30
    [InlineData(15, 28)]
    public async Task Availability_generates_slots_between_8_and_15(int frequency, int expectedSlots)
    {
        var slots = await AvailabilityAsync(Seed.LargeRoomDetailId, frequency);

        Assert.Equal(expectedSlots, slots.Count);
        Assert.Equal("08:00", slots[0].time);
        Assert.All(slots, s => Assert.True(s.isAvailable));
    }

    [Fact]
    public async Task Availability_marks_slots_covered_by_a_booking_as_unavailable()
    {
        await CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "10:00", "11:00"));

        var slots = await AvailabilityAsync(Seed.LargeRoomDetailId, 30);

        Assert.False(slots.Single(s => s.time == "10:00").isAvailable);
        Assert.False(slots.Single(s => s.time == "10:30").isAvailable);
        Assert.True(slots.Single(s => s.time == "09:30").isAvailable);
        Assert.True(slots.Single(s => s.time == "11:00").isAvailable); // end is exclusive
    }

    [Fact]
    public async Task Availability_ignores_bookings_of_other_resources_and_days()
    {
        await CreateAsync(NewBooking(Seed.SmallRoomDetailId, Seed.AdminEmployeeId, "10:00", "11:00"));
        await CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "10:00", "11:00") with
        {
            startDatetime = Day.AddDays(1).AddHours(10),
            endDatetime = Day.AddDays(1).AddHours(11),
        });

        var slots = await AvailabilityAsync(Seed.LargeRoomDetailId, 60);

        Assert.All(slots, s => Assert.True(s.isAvailable));
    }

    [Fact]
    public async Task Availability_detects_partial_overlap_within_a_slot()
    {
        await CreateAsync(NewBooking(Seed.LargeRoomDetailId, Seed.AdminEmployeeId, "10:15", "10:45"));

        var slots = await AvailabilityAsync(Seed.LargeRoomDetailId, 60);

        Assert.False(slots.Single(s => s.time == "10:00").isAvailable);
    }

    // DO NOT un-skip until the loop is guarded: with frequency <= 0 the loop never ends and
    // allocates until the process runs out of memory, which would hang the whole test run.
    [Theory(Skip = KnownIssue.Prefix + "S4 — frequency <= 0 causes an infinite loop. Do not run until fixed.")]
    [InlineData("0")]
    [InlineData("-15")]
    public async Task Availability_rejects_non_positive_frequency(string frequency)
    {
        await using var db = Database.CreateContext();
        var (ok, _, _) = await new BookingService(db).GetAvailability(new Availability(Seed.LargeRoomDetailId, "2030-01-15", frequency), default);

        Assert.False(ok);
    }

    [Fact(Skip = KnownIssue.Prefix + "S4 — non-numeric frequency throws FormatException (500) instead of returning an error.")]
    public async Task Availability_rejects_non_numeric_frequency()
    {
        await using var db = Database.CreateContext();
        var (ok, _, _) = await new BookingService(db).GetAvailability(new Availability(Seed.LargeRoomDetailId, "2030-01-15", "abc"), default);

        Assert.False(ok);
    }
}
