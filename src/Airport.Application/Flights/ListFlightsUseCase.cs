using Airport.Domain;

namespace Airport.Application;

public record FlightView(Guid Id, string AirlineCode, string FlightCode, string PlaneId, string City,
                         FlightDirection Direction, DateTime ScheduledUtc, FlightStatus Status);

public sealed class ListFlightsUseCase
{
    private readonly IRepository<Flight> _flights;
    public ListFlightsUseCase(IRepository<Flight> flights) => _flights = flights;

    public async Task<IReadOnlyList<FlightView>> HandleAsync()
    {
        var all = await _flights.ListAsync();
        return all
            .OrderBy(f => f.ScheduledUtc)
            .Select(f => new FlightView(f.Id, f.AirlineCode, f.FlightCode, f.PlaneId, f.City, f.Direction, f.ScheduledUtc, f.Status))
            .ToList();
    }
}
