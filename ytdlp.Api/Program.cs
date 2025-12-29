using ytdlp.Services.Interfaces;
using ytdlp.Services;
using System.IO.Abstractions;
using ytdlp.Configs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.WithOrigins("*")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

// Add DI
builder.Services.AddScoped<IDownloadingService, DownloadingService>();
builder.Services.AddScoped<IConfigsServices, ConfigsServices>();
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddScoped<IPathParserService, PathParserService>();
builder.Services.AddScoped<IStartupConfigFixer, StartupConfigFixer>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // HTTP auf Port 8080
});

var app = builder.Build();

// Run startup config fixer
await RunStartupConfigFixerAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAllOrigins");

app.MapControllers();

app.Run();

/// <summary>
/// Executes the startup config fixer to validate and fix existing configs
/// </summary>
static async Task RunStartupConfigFixerAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var fixer = scope.ServiceProvider.GetRequiredService<IStartupConfigFixer>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var result = await fixer.FixAllConfigsAsync();
        
        if (!result.AllSuccess)
        {
            logger.LogWarning(
                "[Startup] Config fixing completed with {ErrorCount} error(s)",
                result.ConfigsWithErrors);
        }
        else
        {
            logger.LogInformation(
                "[Startup] Config fixing completed successfully. Processed: {Total}, Fixed: {Fixed}",
                result.TotalConfigsProcessed,
                result.ConfigsFixed);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Startup] Critical error during config fixing process");
        // Don't throw - app continues even if config fixing fails
    }
}
