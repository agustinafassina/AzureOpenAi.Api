FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

# Copy only the project files first so `dotnet restore` is cached until dependencies change.
COPY OpenAi.Models/*.csproj OpenAi.Models/
COPY OpenAi.Repository/*.csproj OpenAi.Repository/
COPY OpenAi.Services/*.csproj OpenAi.Services/
COPY OpenAi.Api/*.csproj OpenAi.Api/
RUN dotnet restore OpenAi.Api/OpenAi.Api.csproj

COPY . .
RUN dotnet publish OpenAi.Api/OpenAi.Api.csproj -c Release -o /release --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=80
COPY --from=build /release ./
ENTRYPOINT ["dotnet", "OpenAi.Api.dll"]
