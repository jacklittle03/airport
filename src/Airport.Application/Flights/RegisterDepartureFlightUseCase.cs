using System.Linq;
using Airport.Domain;

namespace Airport.Application;

public record RegisterDepartureFlightRequest(
    string AirlineCode, string FlightCode, string ArrivalCity,
    string PlaneId, DateTime ScheduledDepartureUtc);

public sealed class RegisterDepartureFlightUseCase
{
    private readonly IRepository<Flight> _flights;
    public RegisterDepartureFlightUseCase(IRepository<Flight> flights) => _flights = flights;

    public async Task<RegisterFlightResponse?> HandleAsync(RegisterDepartureFlightRequest req)
    {
        if (!AirportRules.IsAllowedAirline(req.AirlineCode)) return null;
        if (!AirportRules.IsAllowedCity(req.ArrivalCity))    return null;
        if (!Validation.IsValidFlightCode(req.FlightCode))   return null;
        if (!Validation.IsValidPlaneId(req.PlaneId))         return null;

        var all = await _flights.ListAsync();
        if (all.Any(f => f.PlaneId.Equals(req.PlaneId, StringComparison.OrdinalIgnoreCase)))
            return null;

        var flight = Flight.Create(req.AirlineCode, req.FlightCode, req.PlaneId,
                                   req.ArrivalCity, FlightDirection.Departure, req.ScheduledDepartureUtc);
        await _flights.AddAsync(flight);
        return new RegisterFlightResponse(flight.Id);
    }
}
