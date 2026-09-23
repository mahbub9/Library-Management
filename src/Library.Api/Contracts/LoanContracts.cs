using System.ComponentModel.DataAnnotations;
using Library.Contracts;

namespace Library.Api.Contracts;

public sealed record CheckOutBody
{
    [Range(1, int.MaxValue)]
    public int PatronId { get; init; }

    [Range(1, int.MaxValue)]
    public int BookId { get; init; }
}

public sealed record LoanResponse(
    int LoanId,
    int BookId,
    string Title,
    int PatronId,
    string PatronName,
    DateTimeOffset CheckedOutOn,
    DateTimeOffset? ReturnedOn)
{
    public static LoanResponse From(LoanView loan) => new(
        loan.LoanId,
        loan.BookId,
        loan.Title,
        loan.PatronId,
        loan.PatronName,
        loan.CheckedOutOn.ToDateTimeOffset(),
        loan.ReturnedOn?.ToDateTimeOffset());
}
