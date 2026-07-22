using System.Reflection;
using Kartly.Application;
using Kartly.Application.Products;
using Kartly.Infrastructure;
using Kartly.Infrastructure.Auth;
using Kartly.Infrastructure.Products;
using Microsoft.Extensions.FileProviders;
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

// Local-disk image storage. Uploaded files live under {contentRoot}/media/uploads and are
// served via the /api/media static-file route below; committed seed images live in media/seed.
var mediaRoot = Path.Combine(builder.Environment.ContentRootPath, "media");
Directory.CreateDirectory(mediaRoot);
builder.Services.AddSingleton<IImageStorage>(new LocalImageStorage(mediaRoot));

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

// Serve uploaded and seed product images (anonymous). Kept under /api/* so the Angular dev
// proxy forwards it and it works unchanged in production.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(mediaRoot),
    RequestPath = "/api/media",
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();

// Exposed so the integration test project can boot the app via WebApplicationFactory.
public partial class Program;
