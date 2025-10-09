using System.Linq;
using Airport.Domain;

namespace Airport.Application;

public record BookArrivalRequest(Guid UserId, Guid FlightId, string Seat);
public record BookResponse(Guid BookingId, string FlightCode, string Seat, DateTime WhenUtc, string City);

/// Books an ARRIVAL flight. If a Frequent Flyer takes an already-occupied seat,
/// the displaced traveller is moved to the next available seat by seat order.
/// Users may have at most one ARRIVAL booking.

public sealed class BookArrivalFlightUseCase
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Flight> _flights;
    private readonly IRepository<Booking> _bookings;

    public BookArrivalFlightUseCase(IRepository<User> users, IRepository<Flight> flights, IRepository<Booking> bookings)
    {
        _users = users; _flights = flights; _bookings = bookings;
    }

    public async Task<BookResponse?> HandleAsync(BookArrivalRequest req)
    {
        var user = await _users.GetByIdAsync(req.UserId);
        var flight = await _flights.GetByIdAsync(req.FlightId);
        if (user is null || flight is null || flight.Direction != FlightDirection.Arrival) return null;
        if (!Validation.IsValidSeat(req.Seat)) return null;

        // rule: single arrival booking per user
        var myExisting = (await _bookings.ListAsync())
            .Any(b => b.UserId == req.UserId && b.Direction == FlightDirection.Arrival);
        if (myExisting) return null;

        var allForThisFlight = (await _bookings.ListAsync()).Where(b => b.FlightId == req.FlightId).ToList();

        // seat taken?
        var seatTaken = allForThisFlight.FirstOrDefault(b => b.Seat.Equals(req.Seat, StringComparison.OrdinalIgnoreCase));

        var isFrequentFlyer = user is FrequentFlyer;

        if (seatTaken != null && !isFrequentFlyer)
            return null; // traveller can't take an occupied seat

        if (seatTaken != null && isFrequentFlyer)
        {
            // FF takes the seat; move the displaced traveller to next available seat
            var currentSeats = new HashSet<string>(allForThisFlight.Select(b => b.Seat), StringComparer.OrdinalIgnoreCase);
            var newSeat = SeatOrder.NextSeatsFrom(req.Seat).FirstOrDefault(s => !currentSeats.Contains(s));
            if (newSeat is null) return null; // plane full
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
