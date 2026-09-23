using Library.Service.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Service.Insights;

public sealed record PopularBookRow(int BookId, string Title, string Author, int TimesBorrowed);

public sealed record BorrowedTogetherRow(int BookId, string Title, string Author, int SharedReaders);

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

    public Task<bool> Exists(int bookId, CancellationToken ct) =>
        db.Books.AnyAsync(book => book.Id == bookId, ct);

    public async Task<List<BorrowedTogetherRow>> BorrowedTogether(int bookId, int limit, CancellationToken ct)
    {
        var readers = db.Loans
            .Where(loan => loan.BookId == bookId)
            .Select(loan => loan.PatronId);

        var rows = await db.Loans
            .Where(loan => readers.Contains(loan.PatronId) && loan.BookId != bookId)
            // One row per book and reader, so borrowing a title three times still makes one reader.
            .Select(loan => new { loan.BookId, loan.Book.Title, loan.Book.Author, loan.PatronId })
            .Distinct()
            .GroupBy(pair => new { pair.BookId, pair.Title, pair.Author })
            .Select(group => new
            {
                group.Key.BookId,
                group.Key.Title,
                group.Key.Author,
                SharedReaders = group.Count()
            })
            .OrderByDescending(row => row.SharedReaders)
            .ThenBy(row => row.BookId)
            .Take(limit)
            .ToListAsync(ct);

        return rows.ConvertAll(row =>
            new BorrowedTogetherRow(row.BookId, row.Title, row.Author, row.SharedReaders));
    }
}
