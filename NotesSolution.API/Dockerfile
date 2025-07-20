
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081



FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NotesSolution.API/NotesSolution.API.csproj", "NotesSolution.API/"]
COPY ["NotesSolution.Infrastructure/NotesSolution.Infrastructure.csproj", "NotesSolution.Infrastructure/"]
COPY ["NotesSolution.Core/NotesSolution.Core.csproj", "NotesSolution.Core/"]
COPY ["NotesSolution.Core/NotesSolution.Application.csproj", "NotesSolution.Application/"]
RUN dotnet restore "./NotesSolution.API/NotesSolution.API.csproj"
COPY . .
WORKDIR "/src/NotesSolution.API"
RUN dotnet build "./NotesSolution.API.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NotesSolution.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ./app-images ./images


RUN mkdir -p /app/images
VOLUME ["/app/images"]
ENTRYPOINT ["dotnet", "NotesSolution.API.dll"]