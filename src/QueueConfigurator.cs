using Microsoft.Extensions.DependencyInjection;

namespace PivotalServices.RabbitMQ.Messaging;

public interface IQueueConfigurator
{
    void AddConsumer<T>(string exchangeName,
                        string queueName,
                        bool addDeadLetterQueue = true,
                        Action<QueueConfiguration> configure = null);
    void AddProducer<T>(string exchangeName,
                        string queueName,
                        bool addDeadLetterQueue = true,
                        Action<QueueConfiguration> configure = null);
}

public class QueueConfigurator : IQueueConfigurator
{
    private readonly IServiceCollection services;

    public QueueConfigurator(IServiceCollection services)
    {
        this.services = services;
    }

    public void AddConsumer<T>(string exchangeName,
                               string queueName,
                               bool addDeadLetterQueue = true,
                               Action<QueueConfiguration> configure = null)
    {
        var optionsName = typeof(T).Name;
        services.Configure<QueueConfiguration>(optionsName, cfg =>
        {
            cfg.ExchangeName = exchangeName;
            cfg.QueueName = queueName;
            cfg.AddDeadLetterQueue = addDeadLetterQueue;
        });

        if (configure != null)
        {
            services.PostConfigure<QueueConfiguration>(optionsName, configure);
        }

        services.AddSingleton<IConsumer<T>,Consumer<T>>();
        Global.ConsumerTypes.Add(typeof(IConsumer<T>));
    }

    public void AddProducer<T>(string exchangeName,
                               string queueName,
                               bool addDeadLetterQueue = true,
                               Action<QueueConfiguration> configure = null)
    {
        var optionsName = typeof(T).Name;
        services.Configure<QueueConfiguration>(optionsName, cfg =>
        {
            cfg.ExchangeName = exchangeName;
            cfg.QueueName = queueName;
            cfg.AddDeadLetterQueue = addDeadLetterQueue;
        });

        if (configure != null)
        {
            services.PostConfigure<QueueConfiguration>(optionsName, configure);
        }

        services.AddSingleton<IProducer<T>,Producer<T>>();
        Global.ProducerTypes.Add(typeof(IProducer<T>));
    }
}