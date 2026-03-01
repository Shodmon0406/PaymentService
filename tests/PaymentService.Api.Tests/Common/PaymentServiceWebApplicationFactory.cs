using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PaymentService.Api.Tests.Common;

public sealed class PaymentServiceWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("payment_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FakePaymentProvider:SuccessRate"] = "1.0",
                ["FakePaymentProvider:DelayMs"] = "0",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace db context
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextDescriptor is not null) 
                services.Remove(dbContextDescriptor);
            
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_postgres.GetConnectionString()));

            var hostedServices = services
                .Where(sd => sd.ServiceType == typeof(IHostedService))
                .ToList();
            
            hostedServices.ForEach(hs => services.Remove(hs));
        });
    }
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        return host;
    }
}