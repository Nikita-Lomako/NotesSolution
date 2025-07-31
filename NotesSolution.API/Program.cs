using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using NotesSolution.Core.Interfaces;
using NotesSolution.Infrastructure.Data;
using NotesSolution.Infrastructure.Services;
using NotesSolution.Core.Interfaces.IRepositories;
using NotesSolution.Infrastructure.Repositories;
using FluentValidation;
using NotesSolution.API.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Validation;
using NotesSolution.Application;
using NotesSolution.Application.Services;
using NotesSolution.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// In this section, services are registered before builder.Build():
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("Database connection string not configured");

var jwtSecret = builder.Configuration["ApiSettings:Secret"]
    ?? throw new Exception("JWT secret not configured");

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddScoped<IImageService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var env = provider.GetRequiredService<IWebHostEnvironment>();
    var logger = provider.GetRequiredService<ILogger<LocalImageService>>();
    return new LocalImageService(
        Path.Combine(env.ContentRootPath, config["ImageStorage:Path"] ?? "images"),
        config["ImageStorage:BaseUrl"] ?? "",
        logger
    );
});


// Register CancellationTokenProvider
builder.Services.AddScoped<ICancellationTokenProvider, CancellationTokenProvider>();

// Register NoteRepository
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();

// Register TagHelperService
builder.Services.AddScoped<ITagHelperService, TagHelperService>();

// Register NoteService
builder.Services.AddScoped<INoteService, NoteService>();

// Register TagService
builder.Services.AddScoped<ITagService, TagService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();


// Register validators
builder.Services.AddScoped<IValidator<NoteCreateDto>, NoteCreateDtoValidator>();
builder.Services.AddScoped<IValidator<NoteUpdateDto>, NoteUpdateDtoValidator>();
builder.Services.AddScoped<IValidator<TagDto>, TagDtoValidator>();
builder.Services.AddScoped<IValidator<TagRequestDto>, TagRequestDtoValidator>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingConfig));

// Add Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// Add AuthRepository
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// JWT Authentication
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

//  Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n " +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();

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

// Map NoteEndpoints
app.MapNoteEndpoints();
app.MapTagEndpoints();
app.MapAuthEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); 
}

app.Run();
