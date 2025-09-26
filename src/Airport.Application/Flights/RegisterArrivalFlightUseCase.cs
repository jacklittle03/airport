using System.Linq;
using Airport.Domain;

namespace Airport.Application;

public record RegisterArrivalFlightRequest(
    string AirlineCode, string FlightCode, string DepartureCity,
    string PlaneId, DateTime ScheduledArrivalUtc);

public record RegisterFlightResponse(Guid FlightId);

public sealed class RegisterArrivalFlightUseCase
{
    private readonly IRepository<Flight> _flights;
    public RegisterArrivalFlightUseCase(IRepository<Flight> flights) => _flights = flights;

    public async Task<RegisterFlightResponse?> HandleAsync(RegisterArrivalFlightRequest req)
    {
        if (!AirportRules.IsAllowedAirline(req.AirlineCode)) return null;
        if (!AirportRules.IsAllowedCity(req.DepartureCity))   return null;
        if (!Validation.IsValidFlightCode(req.FlightCode))    return null;
        if (!Validation.IsValidPlaneId(req.PlaneId))          return null;

        // Plane ID uniqueness across ALL flights
        var all = await _flights.ListAsync();
        if (all.Any(f => f.PlaneId.Equals(req.PlaneId, StringComparison.OrdinalIgnoreCase)))
            return null;

        var flight = Flight.Create(req.AirlineCode, req.FlightCode, req.PlaneId,
                                   req.DepartureCity, FlightDirection.Arrival, req.ScheduledArrivalUtc);
        await _flights.AddAsync(flight);
        return new RegisterFlightResponse(flight.Id);
    }
}
