using System.Reflection;
using booking.core.Constants;

namespace booking.core.test.Constants;

public class CodesTests
{
    private static Dictionary<string, string> AllCodes() =>
        typeof(Codes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!);

    [Fact]
    public void Ok_code_is_OK()
    {
        Assert.Equal("OK", Codes.Ok);
    }

    [Fact]
    public void Code_values_are_unique()
    {
        // The frontend switches on these strings, so two constants must never share a value.
        var values = AllCodes().Values.ToArray();
        Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Every_error_code_starts_with_ERR_prefix()
    {
        var errors = AllCodes().Where(kv => kv.Key != nameof(Codes.Ok));
        Assert.All(errors, kv => Assert.StartsWith("ERR_", kv.Value));
    }

    [Fact]
    public void Auth_codes_use_ERR_AUTH_prefix()
    {
        Assert.StartsWith("ERR_AUTH_", Codes.InvalidCredentials);
        Assert.StartsWith("ERR_AUTH_", Codes.UserNotFound);
        Assert.StartsWith("ERR_AUTH_", Codes.EmailAlreadyExists);
        Assert.StartsWith("ERR_AUTH_", Codes.PasswordWeak);
        Assert.StartsWith("ERR_AUTH_", Codes.EmailRequired);
        Assert.StartsWith("ERR_AUTH_", Codes.EmailNotConfirmed);
    }
}
