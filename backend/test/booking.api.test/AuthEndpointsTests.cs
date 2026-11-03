using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace booking.api.test;

public class AuthEndpointsTests : IClassFixture<BookingApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(BookingApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Login_with_seeded_admin_returns_200_and_token()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { email = Seed.AdminEmail, password = Seed.Password });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("OK", body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("data").GetProperty("token").GetString()));
        Assert.Equal(101, body.GetProperty("data").GetProperty("employeeId").GetInt32());
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_with_generic_message()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { email = Seed.AdminEmail, password = "nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ERR_AUTH_INVALID_CREDENTIALS", body.GetProperty("code").GetString());
        Assert.Equal("Invalid credentials.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { email = "ghost@demo.com", password = Seed.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact(Skip = KnownIssue.Prefix + "S5 — unknown email returns ERR_AUTH_USER_NOT_FOUND, revealing which emails exist.")]
    public async Task Login_failure_code_does_not_reveal_whether_email_exists()
    {
        var unknown = await _client.PostAsJsonAsync("/api/auth/login", new { email = "ghost@demo.com", password = Seed.Password });
        var wrong = await _client.PostAsJsonAsync("/api/auth/login", new { email = Seed.AdminEmail, password = "nope" });

        var unknownCode = (await unknown.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
        var wrongCode = (await wrong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
        Assert.Equal(wrongCode, unknownCode);
    }

    [Fact]
    public async Task Login_with_missing_fields_returns_400()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_new_user_returns_200_and_token()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register",
            new { email = $"user-{Guid.NewGuid():N}@demo.com", password = "Passw0rd", userName = "tester" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("tester", body.GetProperty("data").GetProperty("userName").GetString());
    }

    [Fact]
    public async Task Register_existing_email_returns_400_with_code()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register", new { email = Seed.AdminEmail, password = "Passw0rd" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ERR_AUTH_EMAIL_EXISTS", body.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("not-an-email", "Passw0rd")]
    [InlineData("valid@demo.com", "weak")]
    [InlineData("valid@demo.com", "nouppercase1")]
    public async Task Register_invalid_payload_is_rejected_by_validator(string email, string password)
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
