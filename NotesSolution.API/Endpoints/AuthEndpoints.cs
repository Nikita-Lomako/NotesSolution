using System.Threading;
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
            [FromBody] LoginRequestDto model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var loginResponse = await authService.LoginAsync(model, cancellationToken);
                if (loginResponse == null)
                {
                    return Results.BadRequest(new { Error = "Username or password is incorrect" });
                }
                return Results.Ok(loginResponse);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred during login", statusCode: 500);
            }
        }

        private static async Task<IResult> Register(
            [FromServices] IAuthService authService,
            [FromBody] RegistrationRequestDto model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var registerResponse = await authService.RegisterAsync(model, cancellationToken);
                if (registerResponse == null || string.IsNullOrEmpty(registerResponse.Name))
                {
                    return Results.BadRequest(new { Error = "Registration failed. Please check provided information." });
                }
                return Results.Ok(registerResponse);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499); // Client Closed Request
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred during registration", statusCode: 500);
            }
        }
    }
}
