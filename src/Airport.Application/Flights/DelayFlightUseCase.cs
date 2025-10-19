using System.Linq;
using Airport.Domain;

namespace Airport.Application;

/// Request to delay a flight identified by FlightCode (e.g., "QFA251").
/// If the flight is an ARRIVAL, the paired DEPARTURE (same plane, '...D') is
/// also delayed by the same duration, and all related bookings are updated.
public record DelayFlightRequest(string FlightCode, TimeSpan DelayBy);

/// Response with the updated flight and (optionally) the updated paired departure.
public record DelayFlightResponse(FlightView UpdatedFlight, FlightView? UpdatedPairedDeparture);

public sealed class DelayFlightUseCase
{
    private readonly IRepository<Flight> _flights;
    private readonly IRepository<Booking> _bookings;

    public DelayFlightUseCase(IRepository<Flight> flights, IRepository<Booking> bookings)
    {
        _flights = flights;
        _bookings = bookings;
    }

    public async Task<DelayFlightResponse?> HandleAsync(DelayFlightRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FlightCode) || request.DelayBy <= TimeSpan.Zero)
            return null;

        // Find the target flight
        var allFlights = await _flights.ListAsync();
        var flight = allFlights.FirstOrDefault(f =>
            string.Equals(f.FlightCode, request.FlightCode, StringComparison.OrdinalIgnoreCase));

        if (flight is null) return null;

        // 1) Delay the selected flight and persist
        flight.DelayBy(request.DelayBy);
        await _flights.UpdateAsync(flight);

        // 2) Update all bookings for this flight to the new time
        await UpdateBookingsWhenUtcAsync(flight.Id, flight.ScheduledUtc);

        Flight? paired = null;

        // 3) If ARRIVAL, find paired DEPARTURE (toggle plane suffix A<->D) and delay it too
        if (flight.Direction == FlightDirection.Arrival && flight.PlaneId.Length >= 1)
        {
            var prefix = flight.PlaneId[..^1]; // everything except last char
            var expectedDeparturePlaneId = prefix + 'D';

            paired = allFlights.FirstOrDefault(f =>
                f.Direction == FlightDirection.Departure &&
                string.Equals(f.PlaneId, expectedDeparturePlaneId, StringComparison.OrdinalIgnoreCase));

            if (paired is not null)
            {
                paired.DelayBy(request.DelayBy);
                await _flights.UpdateAsync(paired);

                // Update all bookings for the paired departure as well
                await UpdateBookingsWhenUtcAsync(paired.Id, paired.ScheduledUtc);
            }
        }

        // Shape response views
        var updated = new FlightView(
            flight.Id, flight.AirlineCode, flight.FlightCode, flight.PlaneId,
            flight.City, flight.Direction, flight.ScheduledUtc, flight.Status);

        FlightView? updatedPair = paired is null ? null
            : new FlightView(
                paired.Id, paired.AirlineCode, paired.FlightCode, paired.PlaneId,
                paired.City, paired.Direction, paired.ScheduledUtc, paired.Status);

        return new DelayFlightResponse(updated, updatedPair);
    }

    private async Task UpdateBookingsWhenUtcAsync(Guid flightId, DateTime newWhenUtc)
    {
        var allBookings = await _bookings.ListAsync();
        var toUpdate = allBookings.Where(b => b.FlightId == flightId).ToList();

        foreach (var b in toUpdate)
        {
            b.UpdateWhenUtc(newWhenUtc);
            await _bookings.UpdateAsync(b);
        }
    }
}
