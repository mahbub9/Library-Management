using Grpc.Core;
using Library.Contracts;
using Library.Service.Insights;

namespace Library.Service.Rpc;

public sealed class InsightsEndpoint(
    BookInsights books,
    PatronInsights patrons) : InsightsService.InsightsServiceBase
{
    private const int DefaultLimit = 10;
    private const int MaxLimit = 100;

    public override async Task<PopularBookList> MostBorrowedBooks(
        WindowedTopRequest request, ServerCallContext context)
    {
        var (from, to) = Window(request);
        var rows = await books.MostBorrowed(Limit(request.Limit), from, to, context.CancellationToken);

        var response = new PopularBookList();
        response.Books.Add(rows.Select(row => new PopularBook
        {
            BookId = row.BookId,
            Title = row.Title,
            Author = row.Author,
            TimesBorrowed = row.TimesBorrowed
        }));

        return response;
    }

    public override async Task<ActivePatronList> MostActivePatrons(
        WindowedTopRequest request, ServerCallContext context)
    {
        var (from, to) = Window(request);
        var rows = await patrons.MostActive(Limit(request.Limit), from, to, context.CancellationToken);

        var response = new ActivePatronList();
        response.Patrons.Add(rows.Select(row => new ActivePatron
        {
            PatronId = row.PatronId,
            Name = row.Name,
            BooksBorrowed = row.BooksBorrowed
        }));

        return response;
    }

    // This schema gives limit no explicit presence, so zero means "not supplied".
    private static int Limit(int requested) => requested switch
    {
        0 => DefaultLimit,
        < 0 => throw Invalid("limit must be greater than zero."),
        > MaxLimit => MaxLimit,
        _ => requested
    };

    private static (DateTimeOffset? From, DateTimeOffset? To) Window(WindowedTopRequest request)
    {
        var from = request.From?.ToDateTimeOffset();
        var to = request.To?.ToDateTimeOffset();

        if (from > to)
        {
            throw Invalid("from must not be later than to.");
        }

        return (from, to);
    }

    private static RpcException Invalid(string message) =>
        new(new Status(StatusCode.InvalidArgument, message));
}
