using System.Net;
using System.Net.Http.Json;
using Library.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Library.Tests.Functional;

[Collection(LibraryTests.Name)]
public class InsightEndpointTests(LibraryTestHost host) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(host.ResetAndSeedAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task The_most_borrowed_books_come_back_ranked()
    {
        var ranked = await host.ApiClient
            .GetFromJsonAsync<PopularBookResponse[]>("/api/books/most-borrowed?limit=3");

        ranked.ShouldNotBeNull();
        ranked[0].Title.ShouldBe("The Pragmatic Programmer");
        ranked[0].TimesBorrowed.ShouldBe(5);

        // Books 2 and 3 are both borrowed four times, so this also pins the id tie-break.
        ranked.Select(book => book.BookId).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task The_most_active_patrons_come_back_ranked()
    {
        var ranked = await host.ApiClient
            .GetFromJsonAsync<ActivePatronResponse[]>("/api/patrons/most-active?limit=3");

        ranked.ShouldNotBeNull();
        ranked[0].Name.ShouldBe("James Bennett");
        ranked[0].BooksBorrowed.ShouldBe(7);
    }

    [Fact]
    public async Task Borrowed_together_counts_readers_rather_than_loans()
    {
        var together = await host.ApiClient
            .GetFromJsonAsync<BorrowedTogetherResponse[]>("/api/books/1/borrowed-together?limit=5");

        together.ShouldNotBeNull();
        together.ShouldAllBe(book => book.BookId != 1);

        // Book 4 is in four loans among the readers of book 1, but only two people borrowed it.
        together.Single(book => book.BookId == 4).SharedReaders.ShouldBe(2);
        together[0].SharedReaders.ShouldBe(4);
    }

    [Fact]
    public async Task Borrowed_together_for_a_book_that_is_not_in_the_catalogue_is_rejected()
    {
        var response = await host.ApiClient.GetAsync("/api/books/999/borrowed-together");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_limit_outside_the_allowed_range_is_rejected()
    {
        var response = await host.ApiClient.GetAsync("/api/books/1/borrowed-together?limit=0");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_window_that_ends_before_it_starts_is_rejected()
    {
        var response = await host.ApiClient
            .GetAsync("/api/books/most-borrowed?from=2027-01-01&to=2026-01-01");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("01/09/2025")]
    [InlineData("2025-9-1")]
    [InlineData("2025-09-01T10:00:00Z")]
    public async Task A_date_that_is_not_yyyy_mm_dd_is_rejected(string from)
    {
        var response = await host.ApiClient.GetAsync($"/api/books/most-borrowed?from={Uri.EscapeDataString(from)}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.ShouldContainKey("from");
    }

    [Fact]
    public async Task A_window_given_in_yyyy_mm_dd_counts_only_the_loans_inside_it()
    {
        // The seed's first loans are book 1 on 6 September and book 4 on 11 September.
        var ranked = await host.ApiClient
            .GetFromJsonAsync<PopularBookResponse[]>("/api/books/most-borrowed?from=2025-09-01&to=2025-09-10");

        ranked.ShouldNotBeNull();
        ranked.ShouldHaveSingleItem().BookId.ShouldBe(1);
    }
}
