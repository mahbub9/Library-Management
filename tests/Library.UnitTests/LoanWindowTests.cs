using Library.Service.Domain;
using Library.Service.Insights;
using Shouldly;

namespace Library.UnitTests;

public class LoanWindowTests
{
    private static readonly DateTimeOffset March = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly IQueryable<Loan> Loans = new List<Loan>
    {
        new() { Id = 1, CheckedOutOn = March },
        new() { Id = 2, CheckedOutOn = March.AddDays(15) },
        new() { Id = 3, CheckedOutOn = March.AddDays(31) }
    }.AsQueryable();

    private static int[] IdsIn(DateTimeOffset? from, DateTimeOffset? to) =>
        Loans.InWindow(from, to).Select(loan => loan.Id).ToArray();

    [Fact]
    public void An_unbounded_window_keeps_every_loan() =>
        IdsIn(null, null).ShouldBe(new[] { 1, 2, 3 });

    [Fact]
    public void A_loan_checked_out_on_the_from_date_is_included() =>
        IdsIn(March, null).ShouldBe(new[] { 1, 2, 3 });

    [Fact]
    public void A_loan_checked_out_on_the_to_date_is_excluded() =>
        IdsIn(null, March.AddDays(31)).ShouldBe(new[] { 1, 2 });

    [Fact]
    public void Adjacent_windows_never_count_the_same_loan_twice()
    {
        var first = IdsIn(March, March.AddDays(15)).Length;
        var second = IdsIn(March.AddDays(15), March.AddDays(32)).Length;

        (first + second).ShouldBe(3);
    }
}
