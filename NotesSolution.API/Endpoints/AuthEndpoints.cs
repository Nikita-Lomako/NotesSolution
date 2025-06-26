using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NotesSolution.Core.Dtos;
using NotesSolution.Core.Interfaces.IRepositories;
using System.Net;

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
            [FromServices] IAuthRepository authRepo,
            [FromBody] LoginRequestDto model)
        {
            var loginResponse = await authRepo.Login(model);
            if (loginResponse == null)
            {
                return Results.BadRequest(new { Error = "Username or password is incorrect" });
            }
            return Results.Ok(loginResponse);
        }

        private static async Task<IResult> Register(
            [FromServices] IAuthRepository authRepo,
            [FromBody] RegistrationRequestDto model)
        {
            if (!authRepo.IsUniqueUser(model.UserName))
            {
                return Results.BadRequest(new { Error = "Username already exists" });
            }
            var registerResponse = await authRepo.Register(model);
            if (registerResponse == null || string.IsNullOrEmpty(registerResponse.Name))
            {
                return Results.BadRequest(new { Error = "Registration failed. Please check provided information." });
            }
            return Results.Ok(registerResponse);
        }
    }
} 