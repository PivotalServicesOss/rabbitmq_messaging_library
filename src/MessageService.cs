using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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
        lifetime.ApplicationStopping.Register(StopConsumption);
    }

    IList<object> GetAllConsumers()
    {
        var consumers = new List<object>();
        var types = Assembly.GetAssembly(typeof(MessageService)).GetTypes();
        foreach (var type in types)
        {
            if (type.Name.Contains("Consumer"))
            {
                var instance = this.provider.GetRequiredService(type);
                consumers.Add(instance);
            }
        }
        return consumers;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        StartConsumption();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopConsumption();
        return Task.CompletedTask;
    }

    private void StartConsumption()
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

    private void StopConsumption()
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
                stopped = true;
            }
        }
    }
}
