using ytdlp.Services.Interfaces;
using ytdlp.Services;
using System.IO.Abstractions;
using ytdlp.Configs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

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
builder.Services.AddScoped<ICookiesService, CookiesService>();
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddScoped<IPathParserService, PathParserService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
// builder.Services.AddScoped<IStartupConfigFixer, StartupConfigFixer>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // HTTP auf Port 8080
});

// Configure PathConfiguration
builder.Services.Configure<PathConfiguration>(options =>
{
    var pathConfig = builder.Configuration.GetSection("PathConfiguration").Get<PathConfiguration>();
    if (pathConfig != null)
    {
        options.Downloads = pathConfig.Downloads;
        options.Archive = pathConfig.Archive;
        options.Config = pathConfig.Config;
        options.Cookies = pathConfig.Cookies;
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAllOrigins");

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
