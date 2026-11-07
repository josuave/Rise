using System.Globalization;

namespace Rise.Domain;

/// <summary>Pure time-of-day presentation: given a moment, produce the greeting and clock text. The clock itself (DateTime.Now) lives in the imperative shell.</summary>
public static class Greeting
{
    public static string For(DateTime now) => $"Good {DayPart(now)}.";

    /// <summary>The automatic greeting with the user's name: "Good morning, Joseph." Blank or whitespace-only means no name, exactly as <see cref="For(DateTime)"/>.</summary>
    public static string For(DateTime now, string? name)
    {
        string? trimmed = name?.Trim();
        return string.IsNullOrEmpty(trimmed) ? For(now) : $"Good {DayPart(now)}, {trimmed}.";
    }

    private static string DayPart(DateTime now) => now.Hour switch
    {
        < 12 => "morning",
        < 18 => "afternoon",
        _ => "evening",
    };

    public static string FormatDate(DateTime now) => now.ToString("dddd, MMMM d", CultureInfo.CurrentCulture);

    public static string FormatTime(DateTime now) => FormatTime(now, use24Hour: false, showSeconds: false);

    public static string FormatTime(DateTime now, bool use24Hour, bool showSeconds)
    {
        string format = (use24Hour, showSeconds) switch
        {
            (true, true) => "HH:mm:ss",
            (true, false) => "HH:mm",
            (false, true) => "h:mm:ss tt",
            (false, false) => "h:mm tt",
        };
        return now.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>The clock's parts, so the hero clock can size the seconds (and meridiem) smaller than the digits. Concatenated, they equal <see cref="FormatTime(DateTime, bool, bool)"/>.</summary>
    public static TimeParts FormatTimeParts(DateTime now, bool use24Hour, bool showSeconds)
    {
        string main = now.ToString(use24Hour ? "HH:mm" : "h:mm", CultureInfo.CurrentCulture);
        string seconds = showSeconds ? ":" + now.ToString("ss", CultureInfo.CurrentCulture) : string.Empty;
        string suffix = use24Hour ? string.Empty : now.ToString(" tt", CultureInfo.CurrentCulture);
        return new TimeParts(main, seconds, suffix);
    }
}

/// <summary>The parts of a formatted clock time: the main digits, the seconds (with leading colon, empty when hidden), and the meridiem (with leading space, empty in 24-hour mode). Concatenated they equal the full formatted time.</summary>
public sealed record TimeParts(string Main, string Seconds, string Suffix);
