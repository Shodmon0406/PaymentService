FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["src/PaymentService.Api/PaymentService.Api.csproj", "src/PaymentService.Api/"]
COPY ["src/PaymentService.Application/PaymentService.Application.csproj", "src/PaymentService.Application/"]
COPY ["src/PaymentService.Domain/PaymentService.Domain.csproj", "src/PaymentService.Domain/"]
COPY ["src/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj", "src/PaymentService.Infrastructure/"]
RUN dotnet restore "src/PaymentService.Api/PaymentService.Api.csproj"
COPY . .
WORKDIR "/src/src/PaymentService.Api"
RUN dotnet build "./PaymentService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PaymentService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.Api.dll"]
