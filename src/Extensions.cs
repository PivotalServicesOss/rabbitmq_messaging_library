using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace PivotalServices.RabbitMQ.Messaging;

public static class Extensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services,
                                                 IConfiguration configuration,
                                                 Action<IQueueConfigurator> configure = null)
    {
        if (!services.Any(d => d.ImplementationType == typeof(MessageService)))
        {
            AddHostedService(services, configuration);
            configure?.Invoke(new QueueConfigurator(services));
        }
        return services;
    }

    static void AddHostedService(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
        services.Configure<ServiceConfiguration>(configuration.GetSection(ServiceConfiguration.ConfigRoot));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MessageService>());
    }
}