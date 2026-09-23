using Library.Service.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("library")
            ?? throw new InvalidOperationException("ConnectionStrings:library is not configured."),
        sql => sql.EnableRetryOnFailure()));

var app = builder.Build();

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
