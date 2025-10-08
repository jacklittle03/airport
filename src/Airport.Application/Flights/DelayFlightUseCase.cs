using System.Linq;
using Airport.Domain;

namespace Airport.Application;

/// Request to delay a flight identified by FlightCode (e.g., "QFA251").
/// If the flight is an ARRIVAL, the paired DEPARTURE (same plane, '...D') is
/// also delayed by the same duration.
public record DelayFlightRequest(string FlightCode, TimeSpan DelayBy);

/// Response with the updated flight and (optionally) the updated paired departure.
public record DelayFlightResponse(FlightView UpdatedFlight, FlightView? UpdatedPairedDeparture);

public sealed class DelayFlightUseCase
{
    private readonly IRepository<Flight> _flights;

    public DelayFlightUseCase(IRepository<Flight> flights) => _flights = flights;

    public async Task<DelayFlightResponse?> HandleAsync(DelayFlightRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FlightCode) || request.DelayBy <= TimeSpan.Zero)
            return null;

        var all = await _flights.ListAsync();
        var flight = all.FirstOrDefault(f => 
            string.Equals(f.FlightCode, request.FlightCode, StringComparison.OrdinalIgnoreCase));

        if (flight is null) return null;

        // Delay the selected flight.
        flight.DelayBy(request.DelayBy);
        await _flights.UpdateAsync(flight);

        Flight? paired = null;

        // If ARRIVAL, try to find paired DEPARTURE by toggling the PlaneId suffix A<->D.
        if (flight.Direction == FlightDirection.Arrival && flight.PlaneId.Length >= 1)
        {
            var prefix = flight.PlaneId[..^1];              // everything except last char
            var expected = prefix + 'D';                    // paired departure plane id

            paired = all.FirstOrDefault(f =>
                f.Direction == FlightDirection.Departure &&
                string.Equals(f.PlaneId, expected, StringComparison.OrdinalIgnoreCase));

            if (paired is not null)
            {
                paired.DelayBy(request.DelayBy);
                await _flights.UpdateAsync(paired);
            }
        }

        // Shape response views.
        var updated = new FlightView(flight.Id, flight.AirlineCode, flight.FlightCode, flight.PlaneId,
                                     flight.City, flight.Direction, flight.ScheduledUtc, flight.Status);

        FlightView? updatedPair = paired is null ? null
            : new FlightView(paired.Id, paired.AirlineCode, paired.FlightCode, paired.PlaneId,
                             paired.City, paired.Direction, paired.ScheduledUtc, paired.Status);

        return new DelayFlightResponse(updated, updatedPair);
    }
}
