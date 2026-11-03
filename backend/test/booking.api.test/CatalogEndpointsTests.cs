using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace booking.api.test;

public class CatalogEndpointsTests : IClassFixture<BookingApiFactory>
{
    private readonly HttpClient _client;

    public CatalogEndpointsTests(BookingApiFactory factory) => _client = factory.CreateClient();

    private static async Task<JsonElement> DataAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

    // ---------- Offices ----------

    [Fact]
    public async Task Get_offices_returns_seeded_offices()
    {
        var res = await _client.GetAsync("/api/office");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var names = (await DataAsync(res)).EnumerateArray().Select(o => o.GetProperty("name").GetString()).ToList();
        Assert.Contains("Main Office", names);
        Assert.Contains("Remote Office", names);
    }

    [Fact]
    public async Task Get_unknown_office_returns_404()
    {
        var res = await _client.GetAsync("/api/office/9999");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Office_create_update_delete_round_trip()
    {
        var create = await _client.PostAsJsonAsync("/api/office", new { id = 0, name = "Lab", image = "https://img/lab.png" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(create.Headers.Location);
        var id = (await DataAsync(create)).GetProperty("id").GetInt32();

        var update = await _client.PutAsJsonAsync($"/api/office/{id}", new { id, name = "Lab 2", image = "https://img/lab2.png" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var get = await _client.GetAsync($"/api/office/{id}");
        Assert.Equal("Lab 2", (await DataAsync(get)).GetProperty("name").GetString());

        var delete = await _client.DeleteAsync($"/api/office/{id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/office/{id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_unknown_office_returns_404()
    {
        var res = await _client.DeleteAsync("/api/office/9999");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ---------- Services ----------

    [Fact]
    public async Task Get_services_returns_services_with_details()
    {
        var res = await _client.GetAsync("/api/services");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var rooms = (await DataAsync(res)).EnumerateArray().Single(s => s.GetProperty("name").GetString() == "Room Booking");
        Assert.Equal(2, rooms.GetProperty("serviceDetails").GetArrayLength());
    }

    [Fact]
    public async Task Get_unknown_service_returns_404()
    {
        var res = await _client.GetAsync("/api/services/9999");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Create_service_returns_201_and_is_retrievable()
    {
        var res = await _client.PostAsJsonAsync("/api/services", new
        {
            name = "Parking",
            officeId = 1,
            description = "Car spots",
            quantity = 5,
            maxBookingsPerWeek = 3,
            bookingPerTime = 1,
            minMax = "60-480",
            imageUrl = (string?)null,
            serviceDetails = new[] { new { id = (int?)null, name = "Spot 1", description = (string?)null, capacity = 1, iconUrl = (string?)null } },
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var id = (await DataAsync(res)).GetProperty("id").GetInt32();
        var get = await _client.GetAsync($"/api/services/{id}");
        Assert.Equal("Spot 1", (await DataAsync(get)).GetProperty("serviceDetails")[0].GetProperty("name").GetString());
    }
}
