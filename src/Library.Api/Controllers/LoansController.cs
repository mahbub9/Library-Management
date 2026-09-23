using Library.Api.Contracts;
using Library.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/loans")]
public sealed class LoansController(
    CirculationService.CirculationServiceClient circulation) : ControllerBase
{
    /// <summary>Lend a book to a patron.</summary>
    [HttpPost]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanResponse>> CheckOut(CheckOutBody body, CancellationToken ct)
    {
        var loan = await circulation.CheckOutAsync(
            new CheckOutRequest { PatronId = body.PatronId, BookId = body.BookId },
            cancellationToken: ct);

        var response = LoanResponse.From(loan);

        return CreatedAtAction(nameof(Get), new { loanId = response.LoanId }, response);
    }

    /// <summary>Look up a loan, whether it is still out or has come back.</summary>
    [HttpGet("{loanId:int}")]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<LoanResponse> Get(int loanId, CancellationToken ct)
    {
        var loan = await circulation.GetLoanAsync(
            new GetLoanRequest { LoanId = loanId }, cancellationToken: ct);

        return LoanResponse.From(loan);
    }

    /// <summary>Take a book back.</summary>
    [HttpPost("{loanId:int}/return")]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<LoanResponse> Return(int loanId, CancellationToken ct)
    {
        var loan = await circulation.CheckInAsync(
            new CheckInRequest { LoanId = loanId }, cancellationToken: ct);

        return LoanResponse.From(loan);
    }
}
