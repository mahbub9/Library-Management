using Library.Service.Circulation;
using Library.Service.Data;
using Library.Service.Domain;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;

namespace Library.Tests.Integration;

[Collection(LibraryTests.Name)]
public class CirculationDeskTests(LibraryTestHost host) : IAsyncLifetime
{
    private static readonly DateTimeOffset March = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    public ValueTask InitializeAsync() => new(host.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Checking_out_records_an_open_loan()
    {
        await using var db = host.NewDbContext();
        var desk = new CirculationDesk(db);
        var (book, patron) = await Given(db, "Pachinko", "Priya Nair");

        var loan = await desk.CheckOut(patron.Id, book.Id, March, default);

        loan.Id.ShouldBeGreaterThan(0);
        loan.CheckedOutOn.ShouldBe(March);
        loan.IsOut.ShouldBeTrue();
    }

    [Fact]
    public async Task A_patron_cannot_check_out_a_title_they_already_have_out()
    {
        await using var db = host.NewDbContext();
        var desk = new CirculationDesk(db);
        var (book, patron) = await Given(db, "Beloved", "James Bennett");

        await desk.CheckOut(patron.Id, book.Id, March, default);

        var broken = await Should.ThrowAsync<CirculationRuleException>(
            () => desk.CheckOut(patron.Id, book.Id, March.AddDays(1), default));

        broken.Rule.ShouldBe(CirculationRule.AlreadyCheckedOut);
    }

    [Fact]
    public async Task Checking_out_an_unknown_title_is_rejected()
    {
        await using var db = host.NewDbContext();
        var desk = new CirculationDesk(db);
        var (_, patron) = await Given(db, "Cloud Atlas", "Michael Chen");

        var broken = await Should.ThrowAsync<CirculationRuleException>(
            () => desk.CheckOut(patron.Id, bookId: 9999, March, default));

        broken.Rule.ShouldBe(CirculationRule.BookNotFound);
    }

    [Fact]
    public async Task A_checkout_that_loses_a_race_is_refused_by_the_index()
    {
        await using var db = host.NewDbContext();
        var (book, patron) = await Given(db, "Beloved", "Ada Okafor");

        await using var raced = RacedBy(rival => rival.CheckOut(patron.Id, book.Id, March, default));

        // The rival lends it after this desk's check has passed, so only the unique index can refuse.
        var broken = await Should.ThrowAsync<CirculationRuleException>(
            () => new CirculationDesk(raced).CheckOut(patron.Id, book.Id, March, default));

        broken.Rule.ShouldBe(CirculationRule.AlreadyCheckedOut);
    }

    private LibraryDbContext RacedBy(Func<CirculationDesk, Task> rival) =>
        host.NewDbContext(new BeforeSaving(async () =>
        {
            await using var other = host.NewDbContext();
            await rival(new CirculationDesk(other));
        }));

    private sealed class BeforeSaving(Func<Task> interrupt) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            await interrupt();

            return result;
        }
    }

    private static async Task<(Book Book, Patron Patron)> Given(
        LibraryDbContext db, string title, string name)
    {
        var book = new Book { Title = title, Author = "Somebody", Pages = 300 };
        var patron = new Patron { Name = name, JoinedOn = new DateOnly(2025, 1, 1) };

        db.AddRange(book, patron);
        await db.SaveChangesAsync();

        return (book, patron);
    }
}
