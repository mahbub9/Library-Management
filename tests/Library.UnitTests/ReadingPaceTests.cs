using Library.Service.Insights;
using Shouldly;

namespace Library.UnitTests;

public class ReadingPaceTests
{
    private static readonly DateTimeOffset CheckedOut = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_book_read_over_ten_days_paces_at_pages_over_days()
    {
        var pace = ReadingPace.Estimate(300, CheckedOut, CheckedOut.AddDays(10));

        pace.DaysHeld.ShouldBe(10);
        pace.PagesPerDay.ShouldBe(30);
    }

    [Fact]
    public void A_book_held_exactly_one_day()
    {
        var pace = ReadingPace.Estimate(258, CheckedOut, CheckedOut.AddHours(24));

        pace.DaysHeld.ShouldBe(1);
        pace.PagesPerDay.ShouldBe(258);
    }

    [Fact]
    public void Part_days_are_measured_not_rounded_up()
    {
        var pace = ReadingPace.Estimate(300, CheckedOut, CheckedOut.AddHours(36));

        pace.DaysHeld.ShouldBe(1.5);
        pace.PagesPerDay.ShouldBe(200);
    }

    [Fact]
    public void A_same_day_return_still_counts_as_one_day_of_reading()
    {
        var pace = ReadingPace.Estimate(209, CheckedOut, CheckedOut.AddHours(3));

        pace.DaysHeld.ShouldBe(1);
        pace.PagesPerDay.ShouldBe(209);
    }
}
