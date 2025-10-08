namespace Airport.Application;

/// Utility to increment seats in a deterministic order:
/// 1A,1B,1C,1D,2A,2B,...,10D
/// Used to find the "next available" seat if a traveller is displaced by a Frequent Flyer.
public static class SeatOrder
{
    public static IEnumerable<string> AllSeats()
    {
        for (int row = 1; row <= 10; row++)
            foreach (var col in new[] { 'A', 'B', 'C', 'D' })
                yield return $"{row}{col}";
    }

    public static IEnumerable<string> NextSeatsFrom(string startSeat)
    {
        // produce sequence starting AFTER startSeat then wrap around
        var all = AllSeats().ToList();
        var idx = all.FindIndex(s => string.Equals(s, startSeat, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) idx = -1;
        for (int i = 1; i < all.Count; i++)
            yield return all[(idx + i) % all.Count];
    }
}
