using System.Globalization;

namespace Rise.Domain;

/// <summary>Pure time-of-day presentation: given a moment, produce the greeting and clock text. The clock itself (DateTime.Now) lives in the imperative shell.</summary>
public static class Greeting
{
    public static string For(DateTime now)
    {
        if (now.Hour < 12)
        {
            return "Good morning.";
        }
        if (now.Hour < 18)
        {
            return "Good afternoon.";
        }
        return "Good evening.";
    }

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
}
