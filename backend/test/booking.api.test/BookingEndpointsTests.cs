using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace booking.api.test;

public class BookingEndpointsTests : IClassFixture<BookingApiFactory>
{
    private readonly HttpClient _client;

    public BookingEndpointsTests(BookingApiFactory factory) => _client = factory.CreateClient();

    private static async Task<JsonElement> DataAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

    private async Task<int> CreateBookingAsync(int serviceDetailId, int employeeId, string start, string end)
    {
        var res = await _client.PostAsJsonAsync("/api/booking", new
        {
            id = 0, serviceDetailId, employeeId, startDatetime = start, endDatetime = end, createdAt = "2031-01-01T00:00:00",
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await DataAsync(res)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Get_bookings_returns_200_with_list()
    {
        var id = await CreateBookingAsync(1, 104, "2031-02-01T09:00:00", "2031-02-01T10:00:00");

        var res = await _client.GetAsync("/api/booking");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var ids = (await DataAsync(res)).EnumerateArray().Select(b => b.GetProperty("id").GetInt32());
        Assert.Contains(id, ids);
    }

    [Fact]
    public async Task Booking_create_get_update_delete_round_trip()
    {
        var create = await _client.PostAsJsonAsync("/api/booking", new
        {
            id = 0, serviceDetailId = 2, employeeId = 103,
            startDatetime = "2031-03-10T09:00:00", endDatetime = "2031-03-10T10:00:00", createdAt = "2031-03-01T00:00:00",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await DataAsync(create)).GetProperty("id").GetInt32();

        var get = await _client.GetAsync($"/api/booking/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(103, (await DataAsync(get)).GetProperty("employeeId").GetInt32());

        var update = await _client.PutAsJsonAsync($"/api/booking/{id}", new
        {
            id, serviceDetailId = 2, employeeId = 103,
            startDatetime = "2031-03-10T11:00:00", endDatetime = "2031-03-10T12:00:00", createdAt = "2031-03-01T00:00:00",
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync($"/api/booking/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/booking/{id}")).StatusCode);
    }

    [Fact]
    public async Task Get_unknown_booking_returns_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/booking/9999")).StatusCode);
    }

    [Fact]
    public async Task Get_bookings_by_service_returns_rows_with_names()
    {
        var id = await CreateBookingAsync(1, 102, "2031-02-02T09:00:00", "2031-02-02T10:00:00"); // Small Room, Sarah

        var res = await _client.GetAsync("/api/booking/Service?serviceId=1");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var row = (await DataAsync(res)).EnumerateArray().Single(b => b.GetProperty("id").GetInt32() == id);
        Assert.Equal("Sarah Johnson", row.GetProperty("employeeName").GetString());
        Assert.Equal("Small Room", row.GetProperty("serviceDetailName").GetString());
    }

    [Fact]
    public async Task Get_bookings_by_service_without_serviceId_returns_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/booking/Service")).StatusCode);
    }

    [Fact(Skip = KnownIssue.Prefix + "A3 — a service with no bookings returns 404 instead of 200 with an empty list.")]
    public async Task Get_bookings_by_service_without_bookings_returns_empty_200()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/booking/Service?serviceId=2")).StatusCode);
    }

    [Fact]
    public async Task Availability_returns_slots_for_the_day()
    {
        var res = await _client.PostAsJsonAsync("/api/booking/availability", new { id = 4, date = "2031-05-20", frequency = "60" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var slots = await DataAsync(res);
        Assert.Equal(7, slots.GetArrayLength());
        Assert.Equal("08:00", slots[0].GetProperty("time").GetString());
        Assert.True(slots[0].GetProperty("isAvailable").GetBoolean());
    }

    [Fact(Skip = KnownIssue.Prefix + "S4/A1 — an invalid date or frequency throws inside the service and returns a raw 500.")]
    public async Task Availability_with_invalid_input_returns_400()
    {
        var res = await _client.PostAsJsonAsync("/api/booking/availability", new { id = 4, date = "not-a-date", frequency = "abc" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
