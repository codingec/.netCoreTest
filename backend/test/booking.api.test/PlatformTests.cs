using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace booking.api.test;

public class PlatformTests : IClassFixture<BookingApiFactory>
{
    private readonly BookingApiFactory _factory;
    private readonly HttpClient _client;

    public PlatformTests(BookingApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Healthz_returns_200_anonymously()
    {
        var res = await _client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Healthy", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cors_allows_origin_from_configuration()
    {
        using var configured = _factory.WithWebHostBuilder(b =>
            b.UseSetting("Cors:AllowedOrigins:0", "http://booking.local"));
        var client = configured.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Options, "/api/office");
        req.Headers.Add("Origin", "http://booking.local");
        req.Headers.Add("Access-Control-Request-Method", "GET");

        var res = await client.SendAsync(req);

        Assert.True(res.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains("http://booking.local", origins);
    }

    [Fact]
    public async Task Swagger_document_is_served()
    {
        var res = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.Content.ReadAsStringAsync();
        Assert.Contains("Booking API", json);
        Assert.Contains("/api/Booking/availability", json);
    }

    [Fact]
    public async Task Cors_allows_the_angular_dev_origin()
    {
        var req = new HttpRequestMessage(HttpMethod.Options, "/api/office");
        req.Headers.Add("Origin", "http://localhost:4200");
        req.Headers.Add("Access-Control-Request-Method", "GET");

        var res = await _client.SendAsync(req);

        Assert.True(res.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains("http://localhost:4200", origins);
    }

    [Fact]
    public async Task Cors_does_not_allow_other_origins()
    {
        var req = new HttpRequestMessage(HttpMethod.Options, "/api/office");
        req.Headers.Add("Origin", "https://evil.example");
        req.Headers.Add("Access-Control-Request-Method", "GET");

        var res = await _client.SendAsync(req);

        Assert.False(res.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory(Skip = KnownIssue.Prefix + "S1 — no [Authorize] on any controller; every endpoint is anonymous.")]
    [InlineData("GET", "/api/booking")]
    [InlineData("DELETE", "/api/office/2")]
    [InlineData("DELETE", "/api/services/2")]
    [InlineData("POST", "/api/booking/availability")]
    public async Task Protected_endpoints_require_a_token(string method, string url)
    {
        var res = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
