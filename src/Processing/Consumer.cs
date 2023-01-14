using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace PivotalServices.RabbitMQ.Messaging;

public delegate void MessageReceivedDelegate<T>(InboundMessage<T> message);

public interface IConsumer
{
    void StartConsumption();
    void StopConsumption();
}

public interface IConsumer<T> : IProcessor<T>, IConsumer
{
    event MessageReceivedDelegate<T> MessageReceived;
    void Acknowledge(InboundMessage<T> message);
    void Reject(InboundMessage<T> message);
}

public class Consumer<T> : IConsumer<T>
{
    public event MessageReceivedDelegate<T> MessageReceived;
    private readonly IConnectionBuilder<T> connectionBuilder;
    private readonly ILogger<Consumer<T>> logger;
    private string consumerTag;
    EventingBasicConsumer consumer;
    protected readonly static object locker = new object();

    public Consumer(IConnectionBuilder<T> connectionBuilder, ILogger<Consumer<T>> logger)
    {
        this.connectionBuilder = connectionBuilder;
        this.logger = logger;
    }

    public void StartConsumption()
    {
        connectionBuilder.InitializeConnection();
        consumer = new EventingBasicConsumer(connectionBuilder.CurrentChannel);
        consumer.Received += On_MessageReceived;
        consumerTag = connectionBuilder.CurrentChannel.BasicConsume(connectionBuilder.CurrentQueueName,
                                                                    false,
                                                                    consumer);
        logger.LogInformation("Started listening for Message[{message}] from Queue[{queue}], Exchange[{exchange}]",
                                typeof(T).Name,
                                connectionBuilder.CurrentQueueName,
                                connectionBuilder.CurrentExchangeName);
    }

    public void On_MessageReceived(object channel, BasicDeliverEventArgs eventArgs)
    {
        lock (locker)
        {
            InboundMessage<T> message = null;
            try
            {
                logger.LogInformation("Received Message[{message}] from Queue[{queue}], Exchange[{exchange}], CorrelationId: {correlation}",
                                typeof(T).Name,
                                connectionBuilder.CurrentQueueName,
                                connectionBuilder.CurrentExchangeName,
                                eventArgs.BasicProperties.CorrelationId);

                message = new InboundMessage<T>(
                    JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(eventArgs.Body.ToArray())),
                    eventArgs.BasicProperties.ReplyTo,
                    eventArgs.BasicProperties.CorrelationId,
                    eventArgs.DeliveryTag
                );
                logger.LogDebug($"Received, {JsonConvert.SerializeObject(message)}");
                MessageReceived?.Invoke(message);
            }
            catch (Exception exception)
            {
                Reject(message);
                logger.LogError(exception, $"Failed processing, so rejecting", message);
            }
        }
    }

    public void StopConsumption()
    {
        if(consumer == null
            || consumerTag == null)
        {
            logger.LogError("Not started yet!, please invoke {method)} to start consumer", nameof(StartConsumption));
            return;
        }

        connectionBuilder.CurrentChannel?.BasicCancel(consumerTag);
        consumer.Received -= On_MessageReceived;
    }

    public void Acknowledge(InboundMessage<T> message)
    {
        connectionBuilder.CurrentChannel?.BasicAck(message.DeliveryTag, false);
    }

    public void Reject(InboundMessage<T> message)
    {
        connectionBuilder.CurrentChannel?.BasicReject(message.DeliveryTag, false);
    }
}