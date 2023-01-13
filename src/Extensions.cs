using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace PivotalServices.RabbitMQ.Messaging;

public static class Extensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services,
                                                 IConfiguration configuration,
                                                 Action<IConfigurator> configure = null)
    {
        if (!services.Any(d => d.ImplementationType == typeof(Service)))
        {
            ConfigureRabbitConnection(services, configuration);
            configure?.Invoke(new Configurator(services));
        }
        return services;
    }

    static void ConfigureRabbitConnection(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
        services.Configure<ServiceConfiguration>(configuration.GetSection(ServiceConfiguration.ConfigRoot));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, Service>());
    }
}