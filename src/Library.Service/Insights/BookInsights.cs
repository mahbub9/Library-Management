using Library.Service.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Service.Insights;

public sealed record PopularBookRow(int BookId, string Title, string Author, int TimesBorrowed);

public sealed class BookInsights(LibraryDbContext db)
{
    public async Task<List<PopularBookRow>> MostBorrowed(
        int limit, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var rows = await db.Loans
            .InWindow(from, to)
            .GroupBy(loan => new { loan.BookId, loan.Book.Title, loan.Book.Author })
            .Select(group => new
            {
                group.Key.BookId,
                group.Key.Title,
                group.Key.Author,
                TimesBorrowed = group.Count()
            })
            .OrderByDescending(row => row.TimesBorrowed)
            .ThenBy(row => row.BookId)
            .Take(limit)
            .ToListAsync(ct);

        return rows.ConvertAll(row =>
            new PopularBookRow(row.BookId, row.Title, row.Author, row.TimesBorrowed));
    }
}
