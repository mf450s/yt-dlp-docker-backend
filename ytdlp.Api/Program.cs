using ytdlp.Services.Interfaces;
using ytdlp.Services;
using System.IO.Abstractions;
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