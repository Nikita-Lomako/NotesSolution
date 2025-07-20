using Microsoft.AspNetCore.Mvc;
using NotesSolution.Application.Dtos;
using NotesSolution.Application.Interfaces;

namespace NotesSolution.API.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api");
            group.MapPost("/login", Login)
                .WithName("Login")
                .Accepts<LoginRequestDto>("application/json")
                .Produces<LoginResponseDto>(200)
                .Produces(400);
            group.MapPost("/register", Register)
                .WithName("Register")
                .Accepts<RegistrationRequestDto>("application/json")
                .Produces<UserDto>(200)
                .Produces(400);
        }

        private static async Task<IResult> Login(
            [FromServices] IAuthService authService,
            [FromBody] LoginRequestDto model)
        {
            var loginResponse = await authService.LoginAsync(model);
            if (loginResponse == null)
            {
                return Results.BadRequest(new { Error = "Username or password is incorrect" });
            }
            return Results.Ok(loginResponse);
        }

        private static async Task<IResult> Register(
            [FromServices] IAuthService authService,
            [FromBody] RegistrationRequestDto model)
        {
            var registerResponse = await authService.RegisterAsync(model);
            if (registerResponse == null || string.IsNullOrEmpty(registerResponse.Name))
            {
                return Results.BadRequest(new { Error = "Registration failed. Please check provided information." });
            }
            return Results.Ok(registerResponse);
        }
    }
} 