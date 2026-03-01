using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PaymentService.Application.Auth;
using PaymentService.Application.Common;
using PaymentService.Application.Features.Services;
using PaymentService.Infrastructure.Auth;
using PaymentService.Infrastructure.PaymentProvider;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddHttpContextAccessor();

        // Auth
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenJGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        // Payment provider: fake implementation wrapped with Polly resilience policies
        services.Configure<FakePaymentProviderOptions>(configuration.GetSection(FakePaymentProviderOptions.SectionName));
        services.AddSingleton<FakePaymentProviderClient>();
        services.AddSingleton<IPaymentProviderClient>(provider =>
            new ResilientPaymentProviderClient(provider.GetRequiredService<FakePaymentProviderClient>()));

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? throw new InvalidOperationException("JwtSettings not configured");

        var key = Encoding.UTF8.GetBytes(jwtSettings.SigningKey);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers["Token-Expired"] = "true";
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdminRole", policy =>
                policy.RequireRole("SuperAdmin", "Admin"))
            .AddPolicy("RequireUserRole", policy =>
                policy.RequireRole("SuperAdmin", "Admin", "User"));

        return services;
    }
}