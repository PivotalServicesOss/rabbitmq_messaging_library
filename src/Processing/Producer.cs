using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace PivotalServices.RabbitMQ.Messaging;

public interface IProducer
{
    void Close();
}

public interface IProducer<T> : IProducer, IDisposable
{
    void Send(OutboundMessage<T> message);
}

public class Producer<T> : Factory<T>, IProducer<T>
{
    ILogger<Producer<T>> logger;
    protected bool disposedValue;
    private bool isQueueBindingRequired;

    public Producer(IOptions<ServiceConfiguration> serviceConfigurationOptions,
                    IOptionsMonitor<QueueConfiguration> queueConfigurationOptions,
                    ILogger<Producer<T>> logger)
            : base(serviceConfigurationOptions,
                    queueConfigurationOptions,
                    logger)

    {
        this.logger = logger;
        isQueueBindingRequired = queueConfiguration.ExchangeType != ExchangeType.Topic
                                    && queueConfiguration.ExchangeType != ExchangeType.Fanout;
    }

    public void Send(OutboundMessage<T> message)
    {
        InitializeConnection("Producer");

        var serializedMessage = JsonConvert.SerializeObject(message.Content);
        var messageBody = Encoding.UTF8.GetBytes(serializedMessage);

        if (message.RouteKeys == null || !message.RouteKeys.Any())
        {
            if (routingKeysFromConfiguration.Any())
            {
                message.RouteKeys = routingKeysFromConfiguration.ToArray();
            }
            else
            {
                message.RouteKeys = new[] { queueName };
            }
        }

        basicProperties.CorrelationId = message.CorrelationId;
        basicProperties.ReplyTo = message.ReplyTo;
        basicProperties.Priority = Convert.ToByte(message.Priority);

        foreach (var routingKey in message.RouteKeys)
        {
            if (isQueueBindingRequired)
            {
                channel.QueueBind(queueName, queueConfiguration.ExchangeName, routingKey.Trim(), null);
            }

            channel.BasicPublish(queueConfiguration.ExchangeName, routingKey.Trim(), basicProperties, messageBody);
            logger.LogDebug($"Sent message {JsonConvert.SerializeObject(message)}");
            logger.LogInformation($"Sent a message with CorreleationId {message.CorrelationId}");
        }
    }

    public void Close()
    {
        CloseConnection("Producer");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                Close();

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
