using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "Library API";
    document.Info.Version = "v1";
    document.Info.Description =
        "Circulation desk operations and borrowing insights for a public library. Every endpoint " +
        "is served by a gRPC call to the library service; this project holds no database connection.";

    return Task.CompletedTask;
}));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();

public sealed class ApiApp;
