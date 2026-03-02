using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Common.Behaviors.Idempotency;

namespace PaymentService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            
            // Register IdempotencyBehavior as a pipeline behavior
            cfg.AddOpenBehavior(typeof(IdempotencyBehavior<,>));
        });
        
        return services;
    }
}