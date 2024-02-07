using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace PivotalServices.RabbitMQ.Messaging;

public interface IProducer<T> : IProcessor<T>
{
    void Send(OutboundMessage<T> message);  
}

public class Producer<T> : IProducer<T>
{
    private readonly IConnectionBuilder<T> connectionBuilder;
    private readonly ILogger<Producer<T>> logger;
    private bool isQueueBindingRequired;


    public Producer(IConnectionBuilder<T> connectionBuilder, ILogger<Producer<T>> logger)
    {
        this.connectionBuilder = connectionBuilder;
        this.logger = logger;
    }

    public void Send(OutboundMessage<T> message)
    {
        connectionBuilder.InitializeConnection();
        logger.LogInformation("Sending Message[{message}] from Queue[{queue}], Exchange[{exchange}], CorrelationId: {correlation}",
                                typeof(T).Name,
                                connectionBuilder.CurrentQueueName,
                                connectionBuilder.CurrentExchangeName,
                                message.CorrelationId);

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
                connectionBuilder.CurrentChannel.QueueBind(connectionBuilder.CurrentQueueName,
                                                           connectionBuilder.CurrentExchangeName,
                                                           routingKey.Trim(),
                                                           null);
            }

            connectionBuilder.CurrentChannel.BasicPublish(connectionBuilder.CurrentExchangeName,
                                                          routingKey.Trim(),
                                                          connectionBuilder.CurrentBasicProperties,
                                                          messageBody);
            logger.LogDebug($"Sent, {JsonConvert.SerializeObject(message)}");
        }
    }
}
