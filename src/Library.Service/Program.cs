using Library.Service.Circulation;
using Library.Service.Data;
using Library.Service.Rpc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(options => options.Interceptors.Add<CirculationRuleInterceptor>());
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("library")
            ?? throw new InvalidOperationException("ConnectionStrings:library is not configured."),
        sql => sql.EnableRetryOnFailure()));

builder.Services.AddScoped<CirculationDesk>();

var app = builder.Build();

app.MapGrpcService<CirculationEndpoint>();

// Migrating at startup keeps the run story simple; production would migrate before the rollout.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await ShelfSeed.ApplyAsync(db);
    }
}

app.Run();

// WebApplicationFactory needs a public type to locate the entry point, and the generated Program
// classes of the two apps would otherwise collide in the test project.
public sealed class ServiceApp;
