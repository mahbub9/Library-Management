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
    public async Task Other_titles_read_by_the_same_patrons_come_back_ranked()
    {
        await using var db = host.NewDbContext();

        var anchor = Book("Domain-Driven Design");
        var second = Book("Refactoring");
        var third = Book("The Pragmatic Programmer");
        var unrelated = Book("Snow Country");
        var ada = Patron("Ada");
        var bo = Patron("Bo");
        var cai = Patron("Cai");
        var solo = Patron("Solo");
        db.AddRange(anchor, second, third, unrelated, ada, bo, cai, solo);
        await db.SaveChangesAsync();

        Lend(db, anchor, ada, [0]);   Lend(db, second, ada, [10]);
        Lend(db, anchor, bo, [0]);    Lend(db, second, bo, [10]);  Lend(db, third, bo, [20]);
        Lend(db, anchor, cai, [0]);   Lend(db, third, cai, [20]);
        Lend(db, unrelated, solo, [0]);   // nobody who read the anchor also read this
        await db.SaveChangesAsync();

        var together = await new BookInsights(db).BorrowedTogether(anchor.Id, 10, default);

        together.Select(row => row.Title).ToArray()
            .ShouldBe(new[] { "Refactoring", "The Pragmatic Programmer" });
        together[0].SharedReaders.ShouldBe(2);
    }

    [Fact]
    public async Task Each_patron_counts_once_however_often_they_borrowed_a_title()
    {
        await using var db = host.NewDbContext();

        var anchor = Book("Moby Dick");
        var reread = Book("Beloved");
        var shared = Book("Cloud Atlas");
        var enthusiast = Patron("Enthusiast");
        var bo = Patron("Bo");
        var cai = Patron("Cai");
        db.AddRange(anchor, reread, shared, enthusiast, bo, cai);
        await db.SaveChangesAsync();

        Lend(db, anchor, enthusiast, [0]);
        Lend(db, reread, enthusiast, [10, 20, 30]);   // one patron, three loans
        Lend(db, anchor, bo, [0]);   Lend(db, shared, bo, [10]);
        Lend(db, anchor, cai, [0]);  Lend(db, shared, cai, [10]);
        await db.SaveChangesAsync();

        var together = await new BookInsights(db).BorrowedTogether(anchor.Id, 10, default);

        together[0].Title.ShouldBe("Cloud Atlas");
        together[0].SharedReaders.ShouldBe(2);
        together[1].Title.ShouldBe("Beloved");
        together[1].SharedReaders.ShouldBe(1);
    }

    [Fact]
    public async Task A_book_exists_only_once_it_is_in_the_catalogue()
    {
        await using var db = host.NewDbContext();

        var book = Book("Beloved");
        db.Add(book);
        await db.SaveChangesAsync();

        var insights = new BookInsights(db);

        (await insights.Exists(book.Id, default)).ShouldBeTrue();
        (await insights.Exists(book.Id + 1, default)).ShouldBeFalse();
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
