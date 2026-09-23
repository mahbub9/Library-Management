namespace Library.Service.Insights;

public sealed record Pace(double DaysHeld, double PagesPerDay);

public static class ReadingPace
{
    public static Pace Estimate(int pages, DateTimeOffset checkedOutOn, DateTimeOffset returnedOn)
    {
        // Floor at a day: a same-afternoon return would otherwise report an absurd pace.
        var daysHeld = Math.Max(1, (returnedOn - checkedOutOn).TotalDays);

        return new Pace(Math.Round(daysHeld, 2), Math.Round(pages / daysHeld, 1));
    }
}
