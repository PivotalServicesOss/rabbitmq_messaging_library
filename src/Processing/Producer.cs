using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace PivotalServices.RabbitMQ.Messaging;

public interface IProducer
{
    void Close();
}

public interface IProducer<T> : IProcessor<T>, IProducer, IDisposable
{
    void Send(OutboundMessage<T> message);  
}

public class Producer<T> : IProducer<T>
{
    private readonly IConnectionBuilder<T> connectionBuilder;
    private readonly ILogger<Producer<T>> logger;
    private bool disposedValue;
    private bool isQueueBindingRequired;


    public Producer(IConnectionBuilder<T> connectionBuilder, ILogger<Producer<T>> logger)
    {
        this.connectionBuilder = connectionBuilder;
        this.logger = logger;
    }

    public void Send(OutboundMessage<T> message)
    {
        connectionBuilder.InitializeConnection(this);

        isQueueBindingRequired = connectionBuilder.CurrentExchangeType != ExchangeType.Topic
                                    && connectionBuilder.CurrentExchangeType != ExchangeType.Fanout;

        var serializedMessage = JsonConvert.SerializeObject(message.Content);
        var messageBody = Encoding.UTF8.GetBytes(serializedMessage);

        if (message.RouteKeys == null || !message.RouteKeys.Any())
        {
            if (connectionBuilder.CurrentRoutingKeys.Any())
            {
                message.RouteKeys = connectionBuilder.CurrentRoutingKeys.ToArray();
            }
            else
            {
                message.RouteKeys = new[] { connectionBuilder.CurrentQueueName };
            }
        }

        connectionBuilder.CurrentBasicProperties.CorrelationId = message.CorrelationId;
        connectionBuilder.CurrentBasicProperties.ReplyTo = message.ReplyTo;
        connectionBuilder.CurrentBasicProperties.Priority = Convert.ToByte(message.Priority);

        foreach (var routingKey in message.RouteKeys)
        {
            if (isQueueBindingRequired)
            {
                connectionBuilder.CurrentChannel.QueueBind(connectionBuilder.CurrentQueueName, connectionBuilder.CurrentExchangeName, routingKey.Trim(), null);
            }

            connectionBuilder.CurrentChannel.BasicPublish(connectionBuilder.CurrentExchangeName, routingKey.Trim(), connectionBuilder.CurrentBasicProperties, messageBody);
            logger.LogDebug($"Sent message {JsonConvert.SerializeObject(message)}");
            logger.LogInformation($"Sent a message with CorreleationId {message.CorrelationId}");
        }
    }

    public void Close()
    {
        connectionBuilder.CloseConnection(this);
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
