namespace Airport.Domain;

public static class AirportRules
{
    // Allowed airline codes
    public static readonly HashSet<string> Airlines = new(StringComparer.OrdinalIgnoreCase)
        { "JST", "QFA", "RXA", "VOZ", "FRE" };

    // Allowed cities + their FF points
    public static readonly Dictionary<string,int> CityPoints =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sydney"] = 1200,
        ["Melbourne"] = 1750,
        ["Rockhampton"] = 1400,
        ["Adelaide"] = 1950,
        ["Perth"] = 3375
    };

    public static bool IsAllowedAirline(string code) => Airlines.Contains(code);
    public static bool IsAllowedCity(string city) => CityPoints.ContainsKey(city);
}
