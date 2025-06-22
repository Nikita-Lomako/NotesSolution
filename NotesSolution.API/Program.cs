using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using NotesSolution.Core.Interfaces;
using NotesSolution.Infrastructure.Data;
using NotesSolution.Infrastructure.Services;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Infrastructure.Repositories;
using FluentValidation;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Validation;
using AutoMapper;
using NotesSolution.Core;
using NotesSolution.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// ��������� DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ����������� ������� �����������
builder.Services.AddScoped<IImageService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var env = provider.GetRequiredService<IWebHostEnvironment>();

    return new LocalImageService(
        Path.Combine(env.ContentRootPath, config["ImageStorage:Path"]),
        config["ImageStorage:BaseUrl"]
    );
});

// Register NoteRepository
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
// Register validators
builder.Services.AddScoped<IValidator<NoteCreateDto>, NoteCreateDtoValidator>();
builder.Services.AddScoped<IValidator<NoteUpdateDto>, NoteUpdateDtoValidator>();
// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingConfig));

//  Swagger
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

// ����� ����� ���������������� ���������

// Map NoteEndpoints
app.MapNoteEndpoints();
app.MapTagEndpoints();

app.Run();
