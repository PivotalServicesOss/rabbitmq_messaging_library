using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace PivotalServices.RabbitMQ.Messaging;

public class MessageService : IHostedService
{
    private readonly IServiceProvider provider;
    private readonly IHostApplicationLifetime lifetime;
    private static readonly object lockObj = new object();
    bool started;
    bool stopped;

    public MessageService(IServiceProvider provider, IHostApplicationLifetime lifetime)
    {
        this.provider = provider;
        this.lifetime = lifetime;

        //To handle gracefiul shutdown
        lifetime.ApplicationStopping.Register(StopConsumerConsumptionAndCloseProducerConnections);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        StartConsumerConsumption();
        return Task.CompletedTask;
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
