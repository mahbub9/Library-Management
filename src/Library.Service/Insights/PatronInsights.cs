using Library.Service.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Service.Insights;

public sealed record ActivePatronRow(int PatronId, string Name, int BooksBorrowed);

public sealed class PatronInsights(LibraryDbContext db)
{
    public async Task<List<ActivePatronRow>> MostActive(
        int limit, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var rows = await db.Loans
            .InWindow(from, to)
            .GroupBy(loan => new { loan.PatronId, loan.Patron.Name })
            .Select(group => new
            {
                group.Key.PatronId,
                group.Key.Name,
                BooksBorrowed = group.Count()
            })
            .OrderByDescending(row => row.BooksBorrowed)
            .ThenBy(row => row.PatronId)
            .Take(limit)
            .ToListAsync(ct);

        return rows.ConvertAll(row =>
            new ActivePatronRow(row.PatronId, row.Name, row.BooksBorrowed));
    }
}
