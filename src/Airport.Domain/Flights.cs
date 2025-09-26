namespace Airport.Domain;

public enum FlightDirection { Arrival, Departure }
public enum FlightStatus { Scheduled, Delayed }

public sealed class Flight
{
    public Guid Id { get; } = Guid.NewGuid();
    public string AirlineCode { get; private set; } = string.Empty;  // e.g., QFA
    public string FlightCode  { get; private set; } = string.Empty;  // e.g., QFA250
    public string PlaneId     { get; private set; } = string.Empty;  // e.g., QFA8A / QFA8D (unique)
    public string City        { get; private set; } = string.Empty;  // allowed cities
    public FlightDirection Direction { get; private set; }
    public DateTime ScheduledUtc { get; private set; }
    public FlightStatus Status { get; private set; } = FlightStatus.Scheduled;

    private Flight() { }

    public static Flight Create(
        string airlineCode, string flightCode, string planeId,
        string city, FlightDirection direction, DateTime scheduledUtc)
    {
        return new Flight
        {
            AirlineCode = airlineCode,
            FlightCode  = flightCode,
            PlaneId     = planeId,
            City        = city,
            Direction   = direction,
            ScheduledUtc = scheduledUtc,
            Status = FlightStatus.Scheduled
        };
    }

    public void DelayBy(TimeSpan delta)
    {
        ScheduledUtc = ScheduledUtc.Add(delta);
        Status = FlightStatus.Delayed;
    }
}
