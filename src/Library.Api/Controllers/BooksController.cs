using System.ComponentModel.DataAnnotations;
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

    /// <summary>What else was borrowed by the people who borrowed this title?</summary>
    [HttpGet("{bookId:int}/borrowed-together")]
    [ProducesResponseType<IReadOnlyList<BorrowedTogetherResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IReadOnlyList<BorrowedTogetherResponse>> BorrowedTogether(
        int bookId, [FromQuery][Range(1, 100)] int limit = 10, CancellationToken ct = default)
    {
        var together = await insights.BorrowedTogetherAsync(
            new BorrowedTogetherRequest { BookId = bookId, Limit = limit }, cancellationToken: ct);

        return [.. together.Books.Select(book =>
            new BorrowedTogetherResponse(book.BookId, book.Title, book.Author, book.SharedReaders))];
    }
}
