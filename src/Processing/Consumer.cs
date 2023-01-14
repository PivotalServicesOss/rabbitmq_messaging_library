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

public interface IConsumer<T> : IProcessor<T>, IDisposable, IConsumer
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
    protected bool disposedValue;
    private string consumerTag;
    protected readonly static object locker = new object();

    public Consumer(IConnectionBuilder<T> connectionBuilder, ILogger<Consumer<T>> logger)
    {
        this.connectionBuilder = connectionBuilder;
        this.logger = logger;
    }

    public void StartConsumption()
    {
        connectionBuilder.InitializeConnection(this);

        var consumer = new EventingBasicConsumer(connectionBuilder.CurrentChannel);

        consumer.Received += (channel, eventArgs) =>
          {
              lock (locker)
              {
                  InboundMessage<T> message = null;

                  try
                  {
                      message = new InboundMessage<T>(
                          JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(eventArgs.Body.ToArray())),
                          eventArgs.BasicProperties.ReplyTo,
                          eventArgs.BasicProperties.CorrelationId,
                          eventArgs.DeliveryTag
                      );

                      logger.LogDebug($"Received message {JsonConvert.SerializeObject(message)}");
                      logger.LogInformation($"Received a message with CorreleationId {message.CorrelationId}");

                      MessageReceived?.Invoke(message);
                  }
                  catch (Exception exception)
                  {
                      Reject(message);
                      logger.LogError(exception, $"Failed processing message, so rejecting", message);
                  }
              }
          };

        consumerTag = connectionBuilder.CurrentChannel.BasicConsume(connectionBuilder.CurrentQueueName, false, consumer);

        logger.LogInformation($"Started Listening, Consumer[{typeof(T).Name}] -> Queue[{connectionBuilder.CurrentQueueName}]");
    }

    public void StopConsumption()
    {
        if(consumerTag != null)
            connectionBuilder.CurrentChannel?.BasicCancel(consumerTag);

        connectionBuilder.CloseConnection(this);
    }

    public void Acknowledge(InboundMessage<T> message)
    {
        connectionBuilder.CurrentChannel?.BasicAck(message.DeliveryTag, false);
    }

    public void Reject(InboundMessage<T> message)
    {
        connectionBuilder.CurrentChannel?.BasicReject(message.DeliveryTag, false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                StopConsumption();

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}