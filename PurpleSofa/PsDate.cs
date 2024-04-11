namespace PurpleSofa;

/// <summary>
///     Date.
/// </summary>
public static class PsDate
{
    /// <summary>
    ///     AddSeconds.
    ///     '0' is default, it's mean to 'utc'.
    /// </summary>
    public static double AddSeconds { get; set; }

    /// <summary>
    ///     Now.
    /// </summary>
    /// <returns>yyyy-MM-ddTHH:mm:ss.fffzzz</returns>
    internal static string Now()
    {
        var dateTimeOffset = DateTimeOffset.UtcNow;
        dateTimeOffset = dateTimeOffset.ToOffset(TimeSpan.FromSeconds(AddSeconds));
        return dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
    }

    /// <summary>
    ///     Not timestamp milli seconds.
    /// </summary>
    /// <returns>milli seconds</returns>
    internal static long NowTimestampMilliSeconds()
    {
        var dateTimeOffset = DateTimeOffset.UtcNow;
        dateTimeOffset = dateTimeOffset.ToOffset(TimeSpan.FromSeconds(AddSeconds));
        return dateTimeOffset.ToUnixTimeMilliseconds();
    }
}