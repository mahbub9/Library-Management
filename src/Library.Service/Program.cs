var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Run();

// WebApplicationFactory needs a public type to locate the entry point, and the generated Program
// classes of the two apps would otherwise collide in the test project.
public sealed class ServiceApp;
