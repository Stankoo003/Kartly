using System.Reflection;
using Kartly.Application;
using Kartly.Infrastructure;
using Kartly.Infrastructure.Auth;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Kartly API", Version = "v1" });

    // Surface the XML <summary> doc comments in the UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // JWT bearer support: adds an "Authorize" button so protected endpoints are testable.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT from /api/auth/login (no 'Bearer ' prefix needed).",
    });
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc, null), [] },
    });
});
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
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Kartly API v1"));
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
