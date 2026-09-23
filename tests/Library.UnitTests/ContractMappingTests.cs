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
}
