using booking.core.DTOs;

namespace booking.core.test.DTOs;

public class DtoTests
{
    [Fact]
    public void LoginRequest_has_value_equality()
    {
        Assert.Equal(new LoginRequest("a@b.com", "x"), new LoginRequest("a@b.com", "x"));
        Assert.NotEqual(new LoginRequest("a@b.com", "x"), new LoginRequest("a@b.com", "y"));
    }

    [Fact]
    public void RegisterRequest_allows_missing_username()
    {
        var dto = new RegisterRequest("a@b.com", "Passw0rd", null);
        Assert.Null(dto.UserName);
    }

    [Fact]
    public void BookingUpsertDto_supports_non_destructive_mutation()
    {
        var start = new DateTime(2030, 1, 15, 10, 0, 0);
        var original = new BookingUpsertDto(0, 1, 101, start, start.AddHours(1), start);

        var moved = original with { startDatetime = start.AddHours(2), endDatetime = start.AddHours(3) };

        Assert.Equal(start, original.startDatetime);
        Assert.Equal(start.AddHours(2), moved.startDatetime);
        Assert.Equal(original.employeeId, moved.employeeId);
    }

    [Fact]
    public void BookingRow_defaults_names_to_empty_strings()
    {
        var row = new BookingRow();
        Assert.Equal(string.Empty, row.EmployeeName);
        Assert.Equal(string.Empty, row.ServiceDetailName);
    }

    [Fact]
    public void AuthResponse_exposes_roles_and_permissions()
    {
        var resp = new AuthResponse("t", DateTime.UtcNow, 1, "admin", new[] { "Admin" }, new[] { "Booking.Read" }, 101);
        Assert.Single(resp.Roles);
        Assert.Contains("Booking.Read", resp.Permissions);
        Assert.Equal(101, resp.employeeId);
    }
}
