using Airport.Domain;

namespace Airport.Application;

public record TicketView(FlightDirection Direction, string FlightCode, string City, DateTime WhenUtc, string Seat);

public sealed class ListMyTicketsUseCase
{
    private readonly IRepository<Booking> _bookings;

    public ListMyTicketsUseCase(IRepository<Booking> bookings) => _bookings = bookings;

    public async Task<IReadOnlyList<TicketView>> HandleAsync(Guid userId)
    {
        var mine = (await _bookings.ListAsync())
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.WhenUtc)
            .Select(b => new TicketView(b.Direction, b.FlightCode, b.City, b.WhenUtc, b.Seat))
            .ToList();
        return mine;
    }
}
