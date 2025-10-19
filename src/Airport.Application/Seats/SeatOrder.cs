namespace Airport.Application;

public static class SeatOrder
{
    /// Returns all seats in row-major order, mainly for validation/reference.
    public static IEnumerable<string> AllSeats()
    {
        for (int row = 1; row <= 10; row++)
            foreach (var col in new[] { 'A', 'B', 'C', 'D' })
                yield return $"{row}{col}";
    }

    /// Given a seat like "6B", yield seats in the SAME COLUMN with increasing row:
    /// 7B, 8B, 9B, 10B, 1B, 2B, ... (wraps), then finally try other columns (optional fallback).
    /// This matches “incremental seat on the row/column”, preserve column, increment row.
    public static IEnumerable<string> NextSeatsSameColumn(string startSeat)
    {
        if (string.IsNullOrWhiteSpace(startSeat) || startSeat.Length < 2) yield break;

        var col = char.ToUpperInvariant(startSeat[^1]);     // A-D
        if (col is < 'A' or > 'D') yield break;

        if (!int.TryParse(startSeat[..^1], out var row)) yield break; // 1-10
        if (row < 1 || row > 10) yield break;

        // same column first, increment rows with wrap
        for (int i = 1; i <= 10 - 1; i++)                   // 9 candidates before we’d come back to original
        {
            var nextRow = ((row - 1 + i) % 10) + 1;
            yield return $"{nextRow}{col}";
        }

        // If plane is almost full in this column, try the other columns in row-major order
        foreach (var c in new[] { 'A', 'B', 'C', 'D' })
        {
            if (c == col) continue;
            for (int r = 1; r <= 10; r++)
                yield return $"{r}{c}";
        }
    }
}


