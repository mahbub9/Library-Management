using Library.Api.Contracts;
using Library.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(InsightsService.InsightsServiceClient insights) : ControllerBase
{
    /// <summary>What are the most borrowed books?</summary>
    [HttpGet("most-borrowed")]
    [ProducesResponseType<IReadOnlyList<PopularBookResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<PopularBookResponse>> MostBorrowed(
        [FromQuery] TopQuery query, CancellationToken ct)
    {
        var ranked = await insights.MostBorrowedBooksAsync(query.ToRequest(), cancellationToken: ct);

        return [.. ranked.Books.Select(book =>
            new PopularBookResponse(book.BookId, book.Title, book.Author, book.TimesBorrowed))];
    }
}
