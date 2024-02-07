using Microsoft.Extensions.DependencyInjection;

namespace PivotalServices.RabbitMQ.Messaging;

public interface IConfigurator
{
    void AddConsumer<T>(string bindingExchangeName,
                        string queueName,
                        bool addDeadLetterQueue = true,
                        Action<QueueConfiguration> configure = null);
    void AddProducer<T>(string bindingExchangeName,
                        string queueName,
                        bool addDeadLetterQueue = true,
                        Action<QueueConfiguration> configure = null);
}

public class Configurator : IConfigurator
{
    private readonly IServiceCollection services;

    public Configurator(IServiceCollection services)
    {
        this.services = services;
    }

    public void AddConsumer<T>(string bindingExchangeName,
                               string queueName,
                               bool addDeadLetterQueue = true,
                               Action<QueueConfiguration> configure = null)
    {
        var optionsName = typeof(T).Name;
        services.Configure<QueueConfiguration>(optionsName, cfg =>
        {
            cfg.BindingExchangeName = bindingExchangeName;
            cfg.QueueName = queueName;
            cfg.ConfigureDeadLetterQueue = addDeadLetterQueue;
        });

        if (configure != null)
        {
            services.PostConfigure<QueueConfiguration>(optionsName, configure);
        }

        services.AddSingleton<IConnectionBuilder<T>, ConnectionBuilder<T>>();
        services.AddSingleton<IConsumer<T>, Consumer<T>>();
        Global.ConsumerTypes.Add(typeof(IConsumer<T>));
    }

    public void AddProducer<T>(string bindingExchangeName,
                               string queueName,
                               bool configureDeadLetterQueue = true,
                               Action<QueueConfiguration> configure = null)
    {
        var optionsName = typeof(T).Name;
        services.Configure<QueueConfiguration>(optionsName, cfg =>
        {
            cfg.BindingExchangeName = bindingExchangeName;
            cfg.QueueName = queueName;
            cfg.ConfigureDeadLetterQueue = configureDeadLetterQueue;
        });

        if (configure != null)
        {
            services.PostConfigure<QueueConfiguration>(optionsName, configure);
        }

        services.AddSingleton<IConnectionBuilder<T>, ConnectionBuilder<T>>();
        services.AddSingleton<IProducer<T>, Producer<T>>();
    }
}