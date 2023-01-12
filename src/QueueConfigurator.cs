
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
    private readonly IServiceCollection services;

    public QueueConfigurator(IServiceCollection services)
    {
        this.services = services;
    }

    public void AddConsumer<T>(string exchangeName, string queueName, 
                                Action<QueueConfiguration> configure = null, 
                                Action<DlxQueueConfiguration> configureDlx = null)
    {
        var optionsName = typeof(T).Name;
        services.Configure<QueueConfiguration>(optionsName, cfg =>
        {
            cfg.ExchangeName = exchangeName;
            cfg.QueueName = queueName;
        });

        if (configure != null)
        {
            services.PostConfigure<QueueConfiguration>(optionsName, configure);
        }

        if (configureDlx != null)
        {
            services.PostConfigure<DlxQueueConfiguration>(optionsName, configureDlx);
        }

        services.AddSingleton<IConsumer<T>,Consumer<T>>();
        Global.ConsumerTypes.Add(typeof(IConsumer<T>));
    }

    public void AddProducer<T>(string exchangeName, string queueName, 
                                Action<QueueConfiguration> configure = null,
                                Action<DlxQueueConfiguration> configureDlx = null)
    {
        var optionsName = typeof(T).Name;
        services.Configure<QueueConfiguration>(optionsName, cfg =>
        {
            cfg.ExchangeName = exchangeName;
            cfg.QueueName = queueName;
        });

        if (configure != null)
        {
            services.PostConfigure<QueueConfiguration>(optionsName, configure);
        }

        if (configureDlx != null)
        {
            services.PostConfigure<DlxQueueConfiguration>(optionsName, configureDlx);
        }

        services.AddSingleton<IProducer<T>,Producer<T>>();
        Global.ProducerTypes.Add(typeof(IProducer<T>));
    }
}