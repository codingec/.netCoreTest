using System.Reflection;
using System.Text.RegularExpressions;
using booking.core.Constants;

namespace booking.core.test.Constants;

public class PermissionsTests
{
    private static string[] AllPermissionValues() =>
        typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

    [Fact]
    public void ClaimType_is_permission()
    {
        Assert.Equal("permission", Permissions.ClaimType);
    }

    [Fact]
    public void Defines_CRUD_permissions_for_every_resource()
    {
        var values = AllPermissionValues();

        foreach (var resource in new[] { "Booking", "Service", "Office" })
        foreach (var action in new[] { "Read", "Create", "Update", "Delete" })
            Assert.Contains($"{resource}.{action}", values);

        Assert.Equal(12, values.Length);
    }

    [Fact]
    public void Permission_values_are_unique()
    {
        var values = AllPermissionValues();
        Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Permission_values_follow_Resource_dot_Action_format()
    {
        var pattern = new Regex(@"^[A-Z][a-z]+\.(Read|Create|Update|Delete)$");
        Assert.All(AllPermissionValues(), v => Assert.Matches(pattern, v));
    }

    [Theory]
    [InlineData(Permissions.Bookings.Read, "Booking.Read")]
    [InlineData(Permissions.Services.Update, "Service.Update")]
    [InlineData(Permissions.Offices.Delete, "Office.Delete")]
    public void Nested_constants_map_to_expected_values(string actual, string expected)
    {
        Assert.Equal(expected, actual);
    }
}
