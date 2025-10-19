namespace Airport.Domain;

public static class SeatHelper
{
    private static readonly char[] Cols = { 'A', 'B', 'C', 'D' };
    private const int MinRow = 1;
    private const int MaxRow = 10;

    private static (int row, char col) Split(string seat)
    {
        // "8B" (request form, no colon) or "8:B" (defensive) both OK
        var s = seat.Replace(":", "").Trim().ToUpperInvariant();
        var row = int.Parse(new string(s.TakeWhile(char.IsDigit).ToArray()));
        var col = s.Last();
        return (row, col);
    }

    private static string Join(int row, char col) => $"{row}{col}"; // no colon (request format)

    /// Returns the next seat in sequence (row stays, column advances; wraps D->A then 10->1)
    public static string NextSeat(string fromSeat)
    {
        var (row, col) = Split(fromSeat);
        var idx = Array.IndexOf(Cols, col);
        if (idx < 0) idx = 0;

        idx++;
        if (idx >= Cols.Length)
        {
            idx = 0;
            row++;
            if (row > MaxRow) row = MinRow;
        }

        return Join(row, Cols[idx]);
    }

    /// Starting *after* startSeat, walk the cabin order until a free seat is found.
    /// Returns null if all seats are occupied (plane full).
    public static string? FindNextAvailable(string startSeat, ISet<string> occupied)
    {
        var total = (MaxRow - MinRow + 1) * Cols.Length; // 40 seats
        var probe = NextSeat(startSeat);                  // start from the *next* seat
        for (int i = 0; i < total - 1; i++)
        {
            if (!occupied.Contains(probe)) return probe;
            probe = NextSeat(probe);
        }
        return null; // full
    }
}
