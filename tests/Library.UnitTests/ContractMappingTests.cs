using System.ComponentModel.DataAnnotations;
using Google.Protobuf.WellKnownTypes;
using Library.Api.Contracts;
using Library.Contracts;
using Shouldly;

namespace Library.UnitTests;

public class ContractMappingTests
{
    [Fact]
    public void A_loan_that_is_still_out_maps_to_a_null_return_date()
    {
        var view = new LoanView
        {
            LoanId = 7,
            BookId = 3,
            Title = "Domain-Driven Design",
            PatronId = 2,
            PatronName = "Tom Reilly",
            CheckedOutOn = Timestamp.FromDateTimeOffset(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero))
        };

        var response = LoanResponse.From(view);

        response.LoanId.ShouldBe(7);
        response.Title.ShouldBe("Domain-Driven Design");
        response.ReturnedOn.ShouldBeNull();
    }

    [Fact]
    public void A_window_that_ends_before_it_starts_is_rejected()
    {
        var query = new TopQuery
        {
            From = new DateOnly(2027, 1, 1),
            To = new DateOnly(2026, 1, 1)
        };

        var failures = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(
            query, new ValidationContext(query), failures, validateAllProperties: true);

        valid.ShouldBeFalse();
        failures.ShouldContain(failure => failure.ErrorMessage == "from must not be later than to.");
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void TopQuery_accepts_a_limit_from_1_to_100(int limit, bool valid) =>
        IsValid(new TopQuery { Limit = limit }).ShouldBe(valid);

    [Theory]
    [InlineData(0, 0)]   // an empty half-open window, not a backwards one
    [InlineData(0, null)]
    [InlineData(null, 0)]
    public void TopQuery_accepts_any_window_that_does_not_run_backwards(int? fromDay, int? toDay) =>
        IsValid(new TopQuery { From = January(fromDay), To = January(toDay) }).ShouldBeTrue();

    [Fact]
    public void TopQuery_sends_its_days_as_utc_midnight_whatever_the_server_time_zone()
    {
        var request = new TopQuery { From = new DateOnly(2025, 9, 1), To = new DateOnly(2026, 1, 1) }.ToRequest();

        request.From.ToDateTimeOffset().ShouldBe(new DateTimeOffset(2025, 9, 1, 0, 0, 0, TimeSpan.Zero));
        request.To.ToDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private static DateOnly? January(int? day) =>
        day is null ? null : new DateOnly(2026, 1, 1).AddDays(day.Value);

    private static bool IsValid(TopQuery query) =>
        Validator.TryValidateObject(query, new ValidationContext(query), null, validateAllProperties: true);
}
