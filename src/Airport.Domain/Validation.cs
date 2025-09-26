using System.Text.RegularExpressions;

namespace Airport.Domain;

public static class Validation
{
    // Name: at least one letter; only letters, spaces, apostrophes, hyphens
    private static readonly Regex NameRx   = new(@"^(?=.*[A-Za-z])[A-Za-z '\-]+$");
    // Age: 0–99
    public static bool IsValidAge(int age) => age is >= 0 and <= 99;

    // Mobile: 10 digits, leading zero
    private static readonly Regex MobileRx = new(@"^0\d{9}$");
    // Email: exactly one @ with chars both sides
    private static readonly Regex EmailRx  = new(@"^[^@\s]+@[^@\s]+$");
    // Password: ≥8, at least one lower, one upper, one digit
    private static readonly Regex PwRx     = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$");

    public static bool IsValidName(string s)     => !string.IsNullOrWhiteSpace(s) && NameRx.IsMatch(s);
    public static bool IsValidMobile(string s)   => MobileRx.IsMatch(s ?? "");
    public static bool IsValidEmail(string s)    => EmailRx.IsMatch(s ?? "");
    public static bool IsValidPassword(string s) => PwRx.IsMatch(s ?? "");

    private static readonly Regex FlightIdRx = new(@"^[A-Z]{2,3}\d{3}$");   // e.g., QFA250
    private static readonly Regex PlaneIdRx  = new(@"^[A-Z]{3}\d+[AD]$");   // e.g., QFA8A or QFA8D

    public static bool IsValidFlightCode(string s) => FlightIdRx.IsMatch(s ?? "");
    public static bool IsValidPlaneId(string s)    => PlaneIdRx.IsMatch(s ?? "");
}
