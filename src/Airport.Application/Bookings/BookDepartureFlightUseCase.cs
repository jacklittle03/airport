using System.Linq;
using Airport.Domain;

namespace Airport.Application;

public record BookDepartureRequest(Guid UserId, Guid FlightId, string Seat);
// NOTE: BookResponse is already defined (in the Arrival use case). We reuse it here.

/// Books a DEPARTURE flight. If a Frequent Flyer takes an already-occupied seat,
/// the displaced traveller is moved to the next available seat by seat order.
/// Users may have at most one DEPARTURE booking.
public sealed class BookDepartureFlightUseCase
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Flight> _flights;
    private readonly IRepository<Booking> _bookings;

    public BookDepartureFlightUseCase(
        IRepository<User> users,
        IRepository<Flight> flights,
        IRepository<Booking> bookings)
    {
        _users = users; _flights = flights; _bookings = bookings;
    }

    public async Task<BookResponse?> HandleAsync(BookDepartureRequest req)
    {
        var user   = await _users.GetByIdAsync(req.UserId);
        var flight = await _flights.GetByIdAsync(req.FlightId);

        // Must exist and be a DEPARTURE flight
        if (user is null || flight is null || flight.Direction != FlightDirection.Departure) return null;

        // Seat format must be valid (e.g., "8B")
        if (!Validation.IsValidSeat(req.Seat)) return null;

        // Rule: single DEPARTURE booking per user
        var myExisting = (await _bookings.ListAsync())
            .Any(b => b.UserId == req.UserId && b.Direction == FlightDirection.Departure);
        if (myExisting) return null;

        // Current bookings on this flight
        var allForThisFlight = (await _bookings.ListAsync())
            .Where(b => b.FlightId == req.FlightId)
            .ToList();

        // Is the requested seat already taken?
        var seatTaken = allForThisFlight
            .FirstOrDefault(b => b.Seat.Equals(req.Seat, StringComparison.OrdinalIgnoreCase));

        var isFrequentFlyer = user is FrequentFlyer;

        // Traveller cannot take an occupied seat
        if (seatTaken is not null && !isFrequentFlyer)
            return null;

        // Frequent Flyer displaces the existing traveller to the next available seat
        if (seatTaken is not null && isFrequentFlyer)
        {
            var occupied = new HashSet<string>(
                allForThisFlight.Select(b => b.Seat),
                StringComparer.OrdinalIgnoreCase);

            var newSeat = SeatHelper.FindNextAvailable(req.Seat, occupied);
            if (newSeat is null) return null; // plane full

            seatTaken.MoveSeat(newSeat);
            await _bookings.UpdateAsync(seatTaken);
        }

        // Frequent Flyer points for the city (same approach as arrival)
        var points = 0;
        if (user is FrequentFlyer)
        {
            if (AirportRules.CityPoints.TryGetValue(flight.City, out var cityPoints))
                points = cityPoints;
        }

        var booking = Booking.Create(req.UserId, flight, req.Seat, points);
        await _bookings.AddAsync(booking);

        return new BookResponse(
            booking.Id,
            flight.FlightCode,
            booking.Seat,
            flight.ScheduledUtc,
            flight.City);
    }
}
