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
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
// builder.Services.AddScoped<IStartupConfigFixer, StartupConfigFixer>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // HTTP auf Port 8080
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

app.Run();