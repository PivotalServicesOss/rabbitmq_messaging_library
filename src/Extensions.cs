using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace PivotalServices.RabbitMQ.Messaging;

public static class Extensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, Action<IQueueConfigurator> configure = null)
    {
        if (!services.Any(d => d.ImplementationType == typeof(MessageService)))
        {
            AddHostedService(services);
            configure?.Invoke(new QueueConfigurator(new QueueConfiguration(), services));
        }
        return services;
    }

    static void AddHostedService(IServiceCollection services)
    {
        services.AddOptions();
        services.AddOptions<ServiceConfiguration>(ServiceConfiguration.ConfigRoot);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MessageService>());
    }
}