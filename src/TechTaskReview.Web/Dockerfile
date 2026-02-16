FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN apt-get update && apt-get install -y libgit2-dev libssh2-1-dev libfontconfig1 libfreetype6 && rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY ["src/TechTaskReview.Domain/TechTaskReview.Domain.csproj", "TechTaskReview.Domain/"]
COPY ["src/TechTaskReview.Application/TechTaskReview.Application.csproj", "TechTaskReview.Application/"]
COPY ["src/TechTaskReview.Infrastructure/TechTaskReview.Infrastructure.csproj", "TechTaskReview.Infrastructure/"]
COPY ["src/TechTaskReview.Web/TechTaskReview.Web.csproj", "TechTaskReview.Web/"]
RUN dotnet restore "TechTaskReview.Web/TechTaskReview.Web.csproj"
COPY src/ .
WORKDIR "/src/TechTaskReview.Web"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "TechTaskReview.Web.dll"]
