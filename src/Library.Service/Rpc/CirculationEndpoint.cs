using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Library.Contracts;
using Library.Service.Circulation;
using Library.Service.Domain;

namespace Library.Service.Rpc;

public sealed class CirculationEndpoint(CirculationDesk desk, ILogger<CirculationEndpoint> log)
    : CirculationService.CirculationServiceBase
{
    public override async Task<LoanView> CheckOut(CheckOutRequest request, ServerCallContext context)
    {
        var loan = await desk.CheckOut(
            request.PatronId, request.BookId, DateTimeOffset.UtcNow, context.CancellationToken);

        // Ids only: a name next to a title is a reading record, and logs are not the place for one.
        log.LogInformation("Patron {PatronId} checked out book {BookId} as loan {LoanId}",
            loan.PatronId, loan.BookId, loan.Id);

        return ToView(loan);
    }

    public override async Task<LoanView> CheckIn(CheckInRequest request, ServerCallContext context)
    {
        var loan = await desk.CheckIn(request.LoanId, DateTimeOffset.UtcNow, context.CancellationToken);

        log.LogInformation("Loan {LoanId} returned after {Days:0.0} days",
            loan.Id, (loan.ReturnedOn!.Value - loan.CheckedOutOn).TotalDays);

        return ToView(loan);
    }

    public override async Task<LoanView> GetLoan(GetLoanRequest request, ServerCallContext context)
    {
        var loan = await desk.GetLoan(request.LoanId, context.CancellationToken);

        return ToView(loan);
    }

    internal static LoanView ToView(Loan loan) => new()
    {
        LoanId = loan.Id,
        BookId = loan.BookId,
        Title = loan.Book.Title,
        PatronId = loan.PatronId,
        PatronName = loan.Patron.Name,
        CheckedOutOn = Timestamp.FromDateTimeOffset(loan.CheckedOutOn),
        ReturnedOn = loan.ReturnedOn is null ? null : Timestamp.FromDateTimeOffset(loan.ReturnedOn.Value)
    };
}
