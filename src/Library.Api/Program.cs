using Library.Api;
using Library.Contracts;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<RpcProblemDetailsHandler>();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "Library API";
    document.Info.Version = "v1";
    document.Info.Description =
        "Circulation desk operations and borrowing insights for a public library. Every endpoint " +
        "is served by a gRPC call to the library service; this project holds no database connection.";

    return Task.CompletedTask;
}));

var library = new Uri(builder.Configuration["Services:Library"]
    ?? throw new InvalidOperationException("Services:Library is not configured."));

builder.Services.AddGrpcClient<CirculationService.CirculationServiceClient>(o => o.Address = library);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

app.Run();

public sealed class ApiApp;
