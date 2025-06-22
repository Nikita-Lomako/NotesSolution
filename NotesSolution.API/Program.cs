using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using NotesSolution.Core.Interfaces;
using NotesSolution.Infrastructure.Data;
using NotesSolution.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Добавляем DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация сервиса изображений
builder.Services.AddScoped<IImageService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var env = provider.GetRequiredService<IWebHostEnvironment>();

    return new LocalImageService(
        Path.Combine(env.ContentRootPath, config["ImageStorage:Path"]),
        config["ImageStorage:BaseUrl"]
    );
});

// Регистрируем Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serving static files (for images)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "images")),
    RequestPath = "/images"
});

app.UseHttpsRedirection();

// Здесь будут регистрироваться эндпоинты

app.Run();
