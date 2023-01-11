
using System;
using Microsoft.Extensions.DependencyInjection;

namespace PivotalServices.RabbitMQ.Messaging;

public interface IQueueConfigurator
{
    void AddConsumer<T>(string exchangeName, string queueName, 
                            Action<QueueConfiguration> configure = null, 
                            Action<DlxQueueConfiguration> configureDlx = null);
    void AddProducer<T>(string exchangeName, string queueName, 
                            Action<QueueConfiguration> configure = null, 
                            Action<DlxQueueConfiguration> configureDlx = null);
}

public class QueueConfigurator : IQueueConfigurator
{
    private QueueConfiguration queueConfiguration;
    private readonly IServiceCollection services;

    public QueueConfigurator(QueueConfiguration queueConfiguration, IServiceCollection services)
    {
        this.queueConfiguration = queueConfiguration;
        this.services = services;
    }

    public void AddConsumer<T>(string exchangeName, string queueName, 
                                Action<QueueConfiguration> configure = null, 
                                Action<DlxQueueConfiguration> configureDlx = null)
    {
        services.Configure<QueueConfiguration>(nameof(T), cfg =>
        {
            cfg.ExchangeName = exchangeName;
            cfg.QueueName = queueName;
        });

        if (configure != null)
        {
            services.PostConfigure<QueueConfiguration>(nameof(T), configure);
        }

        if (configureDlx != null)
        {
            services.PostConfigure<DlxQueueConfiguration>(nameof(T), configureDlx);
        }

        services.AddSingleton<IConsumer<T>,Consumer<T>>();
    }

    public void AddProducer<T>(string exchangeName, string queueName, 
                                Action<QueueConfiguration> configure = null,
                                Action<DlxQueueConfiguration> configureDlx = null)
    {
        services.Configure<QueueConfiguration>(nameof(T), cfg =>
        {
            cfg.ExchangeName = exchangeName;
            cfg.QueueName = queueName;
        });

        if (configure != null)
        {
            services.PostConfigure<QueueConfiguration>(nameof(T), configure);
        }

        if (configureDlx != null)
        {
            services.PostConfigure<DlxQueueConfiguration>(nameof(T), configureDlx);
        }

        services.AddSingleton<IProducer<T>,Producer<T>>();
    }
}