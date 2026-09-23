using Library.Service.Domain;
using Microsoft.EntityFrameworkCore;

namespace Library.Service.Data;

public static class ShelfSeed
{
    private static readonly DateTimeOffset Opening = new(2025, 9, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly (string Title, string Author, int Pages)[] Shelf =
    [
        ("The Pragmatic Programmer", "Andrew Hunt", 352),
        ("Designing Data-Intensive Applications", "Martin Kleppmann", 616),
        ("Domain-Driven Design", "Eric Evans", 560),
        ("Moby Dick", "Herman Melville", 635),
        ("The Left Hand of Darkness", "Ursula K. Le Guin", 304),
        ("Things Fall Apart", "Chinua Achebe", 209),
        ("Pachinko", "Min Jin Lee", 496),
        ("The Remains of the Day", "Kazuo Ishiguro", 258),
        ("Snow Country", "Yasunari Kawabata", 175),
        ("Cloud Atlas", "David Mitchell", 509)
    ];

    private static readonly string[] Members =
    [
        "Helen Marsh",
        "Tom Reilly",
        "Priya Nair",
        "James Bennett",
        "Claire Dunne",
        "Michael Chen"
    ];

    // patron, book, day the loan started, hours it was held (null = still out).
    private static readonly (int Patron, int Book, int Day, double? Hours)[] Circulation =
    [
        // A reading circle: three patrons through the same three titles, weeks apart.
        (1, 1, 5, 14 * 24), (1, 2, 25, 21 * 24), (1, 3, 60, 30 * 24),
        (2, 1, 12, 10 * 24), (2, 2, 40, 18 * 24), (2, 3, 75, 25 * 24),
        (3, 1, 20, 9 * 24),  (3, 2, 55, 16 * 24), (3, 3, 90, 28 * 24),

        // Two more borrowings of the most popular title.
        (4, 1, 30, 7 * 24), (5, 1, 48, 12 * 24),

        // Same patron, same title, three times over: separates reader counts from loan counts.
        (4, 4, 10, 20 * 24), (4, 4, 40, 22 * 24), (4, 4, 70, 15 * 24),

        (5, 5, 15, 8 * 24), (5, 6, 35, 5 * 24), (4, 7, 50, 11 * 24),

        // Exactly one day, and a day and a half, for the fractional-days rule.
        (5, 8, 62, 24), (1, 9, 80, 36),

        (2, 10, 85, 13 * 24), (3, 7, 95, 9 * 24),  (4, 2, 100, 17 * 24),
        (5, 3, 105, 20 * 24), (1, 6, 110, 4 * 24), (2, 5, 115, 6 * 24),
        (3, 10, 120, 14 * 24), (4, 8, 125, 10 * 24), (5, 9, 130, 3 * 24),

        // Still out.
        (1, 4, 135, null), (2, 7, 140, null)
    ];

    public static async Task ApplyAsync(LibraryDbContext db, CancellationToken ct = default)
    {
        if (await db.Books.AnyAsync(ct))
        {
            return;
        }

        var books = Shelf
            .Select(entry => new Book { Title = entry.Title, Author = entry.Author, Pages = entry.Pages })
            .ToList();

        var patrons = Members
            .Select((name, index) => new Patron
            {
                Name = name,
                JoinedOn = DateOnly.FromDateTime(Opening.AddDays(index * -40).Date)
            })
            .ToList();

        db.Books.AddRange(books);
        db.Patrons.AddRange(patrons);
        await db.SaveChangesAsync(ct);

        db.Loans.AddRange(Circulation.Select(entry =>
        {
            var checkedOut = Opening.AddDays(entry.Day);

            return new Loan
            {
                BookId = books[entry.Book - 1].Id,
                PatronId = patrons[entry.Patron - 1].Id,
                CheckedOutOn = checkedOut,
                ReturnedOn = entry.Hours is null ? null : checkedOut.AddHours(entry.Hours.Value)
            };
        }));

        await db.SaveChangesAsync(ct);
    }
}
