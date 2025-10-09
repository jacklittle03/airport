namespace Airport.Domain;

public sealed class Booking
{
    public Guid Id { get; } = Guid.NewGuid();

    public Guid UserId { get; private set; }
    public Guid FlightId { get; private set; }
    public FlightDirection Direction { get; private set; } // Arrival or Departure
    public string Seat { get; private set; } = "";         // e.g., 6B

    // For convenience when printing tickets (denormalized snapshot)
    public string FlightCode { get; private set; } = "";
    public string City { get; private set; } = "";         // Arrival: departure city; Departure: arrival city
    public DateTime WhenUtc { get; private set; }
    public int PointsEarned { get; private set; }          // Points for each flight
    private Booking() { }

    public static Booking Create(Guid userId, Flight flight, string seat, int pointsEarned)
    => new Booking
    {
        UserId = userId,
        FlightId = flight.Id,
        Direction = flight.Direction,
        Seat = seat,
        FlightCode = flight.FlightCode,
        City = flight.City,
        WhenUtc = flight.ScheduledUtc,
        PointsEarned = pointsEarned
    };


    public void MoveSeat(string newSeat) => Seat = newSeat;
}
