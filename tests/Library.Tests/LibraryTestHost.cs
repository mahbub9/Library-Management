using Library.Service.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Testcontainers.MsSql;

namespace Library.Tests;

// One SQL container, both applications hosted in memory, and a real gRPC channel between them.
// TestServer speaks HTTP/2, so the generated client reaches the service with no network and no
// ports, exercising controller, protobuf, service, EF Core and SQL in one pass.
public sealed class LibraryTestHost : IAsyncLifetime
{
    private readonly MsSqlContainer _sql =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private WebApplicationFactory<ServiceApp>? _service;
    private WebApplicationFactory<ApiApp>? _api;

    public HttpClient ApiClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _sql.StartAsync();

        _service = new WebApplicationFactory<ServiceApp>().WithWebHostBuilder(host =>
            host.UseSetting("ConnectionStrings:library", _sql.GetConnectionString()));

        // Touching Server forces the host to build, which runs the migration.
        var serviceServer = _service.Server;

        _api = new WebApplicationFactory<ApiApp>().WithWebHostBuilder(host =>
        {
            host.UseSetting("Services:Library", serviceServer.BaseAddress.ToString());

            // The API keeps its own gRPC clients, deadline and all; only their transport is swapped.
            host.ConfigureServices(services => services.ConfigureAll<HttpClientFactoryOptions>(options =>
                options.HttpMessageHandlerBuilderActions.Add(handler =>
                    handler.PrimaryHandler = serviceServer.CreateHandler())));
        });

        ApiClient = _api.CreateClient();
    }

    public LibraryDbContext NewDbContext(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlServer(_sql.GetConnectionString())
            .AddInterceptors(interceptors)
            .Options);

    /// <summary>Empty every table and restart the identity counters.</summary>
    public async Task ResetAsync()
    {
        await using var db = NewDbContext();

        // DELETE leaves the identity counters alone, so seeded ids would drift on the next run.
        await db.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM Loans;
            DELETE FROM Books;
            DELETE FROM Patrons;
            DBCC CHECKIDENT ('Loans', RESEED, 0);
            DBCC CHECKIDENT ('Books', RESEED, 0);
            DBCC CHECKIDENT ('Patrons', RESEED, 0);
            """);
    }

    /// <summary>Reset, then lay down the shelf the application ships with.</summary>
    public async Task ResetAndSeedAsync()
    {
        await ResetAsync();

        await using var db = NewDbContext();
        await ShelfSeed.ApplyAsync(db);
    }

    public async ValueTask DisposeAsync()
    {
        // Startup can fail at the container, so nothing after it is guaranteed to exist.
        if (_api is not null)
        {
            await _api.DisposeAsync();
        }

        if (_service is not null)
        {
            await _service.DisposeAsync();
        }

        await _sql.DisposeAsync();
    }
}

[CollectionDefinition(LibraryTests.Name)]
public sealed class LibraryTests : ICollectionFixture<LibraryTestHost>
{
    public const string Name = "library";
}
