using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;

namespace PivotalServices.RabbitMQ.Messaging;

public class Service : IHostedService
{
    private readonly IServiceProvider provider;
    private readonly IHostApplicationLifetime lifetime;
    private readonly IOptions<ServiceConfiguration> serviceConfigurationOptions;
    private readonly ILogger<Service> logger;
    private static readonly object lockObj = new object();
    bool started;
    bool stopped;

    public Service(IServiceProvider provider,
                          IHostApplicationLifetime lifetime,
                          IOptions<ServiceConfiguration> serviceConfigurationOptions,
                          ILogger<Service> logger)
    {
        this.provider = provider;
        this.lifetime = lifetime;
        this.serviceConfigurationOptions = serviceConfigurationOptions;
        this.logger = logger;

        //To handle gracefiul shutdown
        lifetime.ApplicationStopping.Register(StopConsumerConsumptionAndCloseProducerConnections);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        CheckRabbitMqConnection();
        StartConsumerConsumption();
        return Task.CompletedTask;
    }

    private void CheckRabbitMqConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = serviceConfigurationOptions.Value.HostName,
            VirtualHost = serviceConfigurationOptions.Value.Vhost,
            UserName = serviceConfigurationOptions.Value.Username,
            Password = serviceConfigurationOptions.Value.Password,
        };

        logger.LogInformation($"Connecting to RabbitMq, {factory.Endpoint}");
        factory.CreateConnection().Close();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopConsumerConsumptionAndCloseProducerConnections();
        return Task.CompletedTask;
    }

    private void StartConsumerConsumption()
    {
        lock (lockObj)
        {
            if (!started)
            {
                var consumers = GetAllConsumers();
                foreach (IConsumer consumer in consumers)
                {
                    consumer.StartConsumption();
                }
                started = true;
            }
        }
    }

    private void StopConsumerConsumptionAndCloseProducerConnections()
    {
        lock (lockObj)
        {
            if (!stopped)
            {
                var consumers = GetAllConsumers();
                foreach (IConsumer consumer in consumers)
                {
                    consumer.StopConsumption();
                }

                var producers = GetAllProducers();
                foreach (IProducer producer in producers)
                {
                    producer.Close();
                }
                stopped = true;
            }
        }
    }

    IList<object> GetAllConsumers()
    {
        var consumers = new List<object>();

        foreach (var consumerType in Global.ConsumerTypes)
        {
            var instance = this.provider.GetRequiredService(consumerType);
            consumers.Add(instance);
        }
        return consumers;
    }

    IList<object> GetAllProducers()
    {
        var producers = new List<object>();

        foreach (var producerType in Global.ProducerTypes)
        {
            var instance = this.provider.GetRequiredService(producerType);
            producers.Add(instance);
        }
        return producers;
    }
}
