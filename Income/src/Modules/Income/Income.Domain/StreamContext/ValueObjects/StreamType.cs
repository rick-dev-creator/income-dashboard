namespace Income.Domain.StreamContext.ValueObjects;

/// <summary>
/// Defines whether a stream represents money flowing in (Income) or out (Outcome).
/// </summary>
internal enum StreamType
{
    /// <summary>Money flowing IN - earnings, revenue, gains</summary>
    Income = 0,

    /// <summary>Money flowing OUT - expenses, costs, burn rate</summary>
    Outcome = 1
}

/// <summary>
/// Represents the direction multiplier for flow calculations.
/// Income = +1 (positive contribution), Outcome = -1 (negative contribution to net).
/// </summary>
internal static class FlowDirection
{
    public static int FromStreamType(StreamType type) => type switch
    {
        StreamType.Income => 1,
        StreamType.Outcome => -1,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static decimal ApplyDirection(decimal amount, StreamType type) =>
        amount * FromStreamType(type);
}
