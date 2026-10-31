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

    public static string FormatDate(DateTime now) => now.ToString("dddd, MMMM d");

    public static string FormatTime(DateTime now) => now.ToString("h:mm tt");
}
