using Library.Service.Data;
using Library.Service.Domain;
using Library.Service.Insights;
using Shouldly;

namespace Library.Tests.Integration;

[Collection(LibraryTests.Name)]
public class InsightQueryTests(LibraryTestHost host) : IAsyncLifetime
{
    private static readonly DateTimeOffset March = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    public ValueTask InitializeAsync() => new(host.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Titles_are_ranked_by_how_often_they_went_out()
    {
        await using var db = host.NewDbContext();

        var popular = Book("The Dispossessed");
        var middling = Book("Snow Country");
        var quiet = Book("The Name of the Rose");
        var reader = Patron("Kenji Watanabe");
        db.AddRange(popular, middling, quiet, reader);
        await db.SaveChangesAsync();

        Lend(db, popular, reader, days: [0, 4, 8, 12, 16]);
        Lend(db, middling, reader, days: [1, 5, 9]);
        Lend(db, quiet, reader, days: [2]);
        await db.SaveChangesAsync();

        var ranked = await new BookInsights(db).MostBorrowed(10, null, null, default);

        ranked.Select(row => row.Title).ToArray()
            .ShouldBe(new[] { "The Dispossessed", "Snow Country", "The Name of the Rose" });
        ranked[0].TimesBorrowed.ShouldBe(5);
    }

    [Fact]
    public async Task Patrons_are_ranked_by_how_much_they_borrowed()
    {
        await using var db = host.NewDbContext();

        var book = Book("Moby Dick");
        var busy = Patron("Helen Marsh");
        var occasional = Patron("James Bennett");
        db.AddRange(book, busy, occasional);
        await db.SaveChangesAsync();

        Lend(db, book, busy, days: [0, 3, 6, 9]);
        Lend(db, book, occasional, days: [1]);
        await db.SaveChangesAsync();

        var ranked = await new PatronInsights(db).MostActive(10, null, null, default);

        ranked[0].Name.ShouldBe("Helen Marsh");
        ranked[0].BooksBorrowed.ShouldBe(4);
        ranked[1].BooksBorrowed.ShouldBe(1);
    }

    [Fact]
    public async Task Only_loans_inside_the_window_are_counted()
    {
        await using var db = host.NewDbContext();

        var book = Book("A Brief History of Time");
        var reader = Patron("Tom Reilly");
        db.AddRange(book, reader);
        await db.SaveChangesAsync();

        Lend(db, book, reader, [-1, 0, 10]);
        await db.SaveChangesAsync();

        var ranked = await new BookInsights(db)
            .MostBorrowed(10, March, March.AddDays(10), default);

        // The loan on the "to" date is excluded and the one before "from" never counted.
        ranked.Single().TimesBorrowed.ShouldBe(1);
    }

    private static Book Book(string title) =>
        new() { Title = title, Author = "Ursula K. Le Guin", Pages = 300 };

    private static Patron Patron(string name) =>
        new() { Name = name, JoinedOn = new DateOnly(2025, 1, 1) };

    private static void Lend(LibraryDbContext db, Book book, Patron patron, int[] days)
    {
        foreach (var day in days)
        {
            db.Add(new Loan
            {
                BookId = book.Id,
                PatronId = patron.Id,
                CheckedOutOn = March.AddDays(day),
                ReturnedOn = March.AddDays(day + 1)
            });
        }
    }
}
