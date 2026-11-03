using booking.core.Models;

namespace booking.core.test.Models;

public class ModelDefaultsTests
{
    [Fact]
    public void Service_starts_with_empty_detail_collection()
    {
        var service = new Service();
        Assert.NotNull(service.ServiceDetails);
        Assert.Empty(service.ServiceDetails);
    }

    [Fact]
    public void User_starts_with_empty_roles_and_claims()
    {
        var user = new User();
        Assert.NotNull(user.Roles);
        Assert.Empty(user.Roles);
        Assert.NotNull(user.Claims);
        Assert.Empty(user.Claims);
        Assert.False(user.EmailConfirmed);
        Assert.False(user.TwoFactorEnabled);
    }

    [Fact]
    public void Role_starts_with_empty_users_and_claims()
    {
        var role = new Role();
        Assert.NotNull(role.Users);
        Assert.Empty(role.Users);
        Assert.NotNull(role.Claims);
        Assert.Empty(role.Claims);
    }

    [Fact]
    public void Booking_navigation_properties_are_optional()
    {
        var booking = new Booking();
        Assert.Null(booking.ServiceDetail);
        Assert.Null(booking.Employee);
        Assert.Null(booking.CreatedAt);
    }
}
