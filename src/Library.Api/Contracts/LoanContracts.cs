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

public sealed record ReadingPaceResponse(
    int LoanId,
    int BookId,
    string Title,
    int Pages,
    int PatronId,
    string PatronName,
    DateTimeOffset CheckedOutOn,
    DateTimeOffset ReturnedOn,
    double DaysHeld,
    double PagesPerDay)
{
    public static ReadingPaceResponse From(ReadingPaceReport report) => new(
        report.LoanId,
        report.BookId,
        report.Title,
        report.Pages,
        report.PatronId,
        report.PatronName,
        report.CheckedOutOn.ToDateTimeOffset(),
        report.ReturnedOn.ToDateTimeOffset(),
        report.DaysHeld,
        report.PagesPerDay);
}
