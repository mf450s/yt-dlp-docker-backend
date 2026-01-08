using ytdlp.Services.Interfaces;
using ytdlp.Services;
using System.IO.Abstractions;
using ytdlp.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging with structured format
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("ytdlp.Services", LogLevel.Debug);
builder.Logging.AddFilter("ytdlp.Api", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "yt-dlp Download API",
        Version = "v1",
        Description = "API for downloading media using yt-dlp with custom configurations"
    });
});
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add DI
builder.Services.AddScoped<IDownloadingService, DownloadingService>();
builder.Services.AddScoped<IConfigsServices, ConfigsServices>();
builder.Services.AddScoped<ICredentialService, ICredentialManagerService>();
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddScoped<IPathParserService, PathParserService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // HTTP auf Port 8080
});

var app = builder.Build();

// Enable Swagger for all environments (not just Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "yt-dlp API v1");
    c.RoutePrefix = "swagger"; // Access at /swagger
});

// Add custom logging middleware early in the pipeline
app.UseMiddleware<LoggingMiddleware>();

app.UseCors("AllowAllOrigins");

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
