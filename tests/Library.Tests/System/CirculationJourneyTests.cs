using System.Net.Http.Json;
using Library.Api.Contracts;
using Shouldly;

namespace Library.Tests.System;

[Collection(LibraryTests.Name)]
public class CirculationJourneyTests(LibraryTestHost host) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(host.ResetAndSeedAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Lending_a_quiet_title_moves_it_up_the_rankings()
    {
        const int QuietBook = 6;   // Things Fall Apart, twice borrowed in the seed

        var before = await host.ApiClient
            .GetFromJsonAsync<PopularBookResponse[]>("/api/books/most-borrowed?limit=3");
        before!.ShouldNotContain(book => book.BookId == QuietBook);

        for (var patron = 1; patron <= 6; patron++)
        {
            var created = await host.ApiClient
                .PostAsJsonAsync("/api/loans", new { patronId = patron, bookId = QuietBook });
            var loan = await created.Content.ReadFromJsonAsync<LoanResponse>();

            await host.ApiClient.PostAsync($"/api/loans/{loan!.LoanId}/return", null);
        }

        var after = await host.ApiClient
            .GetFromJsonAsync<PopularBookResponse[]>("/api/books/most-borrowed?limit=3");

        after![0].BookId.ShouldBe(QuietBook);
        after[0].TimesBorrowed.ShouldBe(8);
    }
}
