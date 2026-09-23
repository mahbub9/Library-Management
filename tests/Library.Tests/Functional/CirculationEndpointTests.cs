using System.Net;
using System.Net.Http.Json;
using Library.Api.Contracts;
using Shouldly;

namespace Library.Tests.Functional;

[Collection(LibraryTests.Name)]
public class CirculationEndpointTests(LibraryTestHost host) : IAsyncLifetime
{
    // Patron 6 has no seeded history, so this class can lend freely.
    private const int Patron = 6;

    public ValueTask InitializeAsync() => new(host.ResetAndSeedAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Checking_out_returns_201_and_a_loan_that_can_be_fetched()
    {
        var created = await host.ApiClient
            .PostAsJsonAsync("/api/loans", new { patronId = Patron, bookId = 5 });

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        created.Headers.Location.ShouldNotBeNull();

        var loan = await created.Content.ReadFromJsonAsync<LoanResponse>();
        loan!.PatronId.ShouldBe(Patron);
        loan.ReturnedOn.ShouldBeNull();

        var fetched = await host.ApiClient.GetFromJsonAsync<LoanResponse>(created.Headers.Location);
        fetched!.LoanId.ShouldBe(loan.LoanId);
    }

    [Fact]
    public async Task A_returned_loan_can_be_fetched_by_id()
    {
        // Loan 18 was held for a day and has come back.
        var loan = await host.ApiClient.GetFromJsonAsync<LoanResponse>("/api/loans/18");

        loan!.LoanId.ShouldBe(18);
        loan.ReturnedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task Checking_out_a_title_the_patron_already_has_gives_409()
    {
        await host.ApiClient.PostAsJsonAsync("/api/loans", new { patronId = Patron, bookId = 7 });

        var again = await host.ApiClient
            .PostAsJsonAsync("/api/loans", new { patronId = Patron, bookId = 7 });

        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_unregistered_patron_gives_404()
    {
        var response = await host.ApiClient
            .PostAsJsonAsync("/api/loans", new { patronId = 9999, bookId = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("GET", "/api/loans/9999")]
    [InlineData("POST", "/api/loans/9999/return")]
    public async Task An_unknown_loan_gives_404(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        var response = await host.ApiClient.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_unusable_request_body_gives_400_before_the_service_is_called()
    {
        var response = await host.ApiClient
            .PostAsJsonAsync("/api/loans", new { patronId = 0, bookId = -3 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returning_a_loan_twice_gives_409()
    {
        var created = await host.ApiClient
            .PostAsJsonAsync("/api/loans", new { patronId = Patron, bookId = 9 });
        var loan = await created.Content.ReadFromJsonAsync<LoanResponse>();

        var first = await host.ApiClient.PostAsync($"/api/loans/{loan!.LoanId}/return", null);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await host.ApiClient.PostAsync($"/api/loans/{loan.LoanId}/return", null);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
