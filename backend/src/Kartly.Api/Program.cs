using Kartly.Application;
using Kartly.Infrastructure;
using Kartly.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Compose the layers: business logic + data-access implementations.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Apply migrations and seed roles + the default admin account on startup.
await KartlyDbInitializer.InitializeAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // In dev the Angular proxy talks plain HTTP to the API; forcing HTTPS
    // redirects would break it. Only redirect outside Development.
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();

// Exposed so the integration test project can boot the app via WebApplicationFactory.
public partial class Program;
