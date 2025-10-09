using System.Linq;
using Airport.Domain;

namespace Airport.Application;

public record BookDepartureRequest(Guid UserId, Guid FlightId, string Seat);

/// Books a DEPARTURE flight with the same FF override rule. One departure per user.
public sealed class BookDepartureFlightUseCase
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Flight> _flights;
    private readonly IRepository<Booking> _bookings;

    public BookDepartureFlightUseCase(IRepository<User> users, IRepository<Flight> flights, IRepository<Booking> bookings)
    {
        _users = users; _flights = flights; _bookings = bookings;
    }

    public async Task<BookResponse?> HandleAsync(BookDepartureRequest req)
    {
        var user = await _users.GetByIdAsync(req.UserId);
        var flight = await _flights.GetByIdAsync(req.FlightId);
        if (user is null || flight is null || flight.Direction != FlightDirection.Departure) return null;
        if (!Validation.IsValidSeat(req.Seat)) return null;

        // rule: single departure booking per user
        var myExisting = (await _bookings.ListAsync())
            .Any(b => b.UserId == req.UserId && b.Direction == FlightDirection.Departure);
        if (myExisting) return null;

        var allForThisFlight = (await _bookings.ListAsync()).Where(b => b.FlightId == req.FlightId).ToList();

        var seatTaken = allForThisFlight.FirstOrDefault(b => b.Seat.Equals(req.Seat, StringComparison.OrdinalIgnoreCase));
        var isFrequentFlyer = user is FrequentFlyer;

        if (seatTaken != null && !isFrequentFlyer) return null;

        if (seatTaken != null && isFrequentFlyer)
        {
            var currentSeats = new HashSet<string>(allForThisFlight.Select(b => b.Seat), StringComparer.OrdinalIgnoreCase);
            var newSeat = SeatOrder.NextSeatsSameColumn(req.Seat).FirstOrDefault(s => !currentSeats.Contains(s));
            if (newSeat is null) return null;
            seatTaken.MoveSeat(newSeat);
            await _bookings.UpdateAsync(seatTaken);
        }

        var points = 0;
        if (user is FrequentFlyer)
        {
            // City is the City property on the Flight
            if (AirportRules.CityPoints.TryGetValue(flight.City, out var cityPoints))
            {
                points = cityPoints;
            }
        }

        var booking = Booking.Create(req.UserId, flight, req.Seat, points);

        await _bookings.AddAsync(booking);

        return new BookResponse(booking.Id, flight.FlightCode, booking.Seat, flight.ScheduledUtc, flight.City);
    }
}
