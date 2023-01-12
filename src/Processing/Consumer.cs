using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

public interface IConsumer<T> : IDisposable, IConsumer
{
    event MessageReceivedDelegate<T> MessageReceived;
    void Acknowledge(InboundMessage<T> message);
    void Reject(InboundMessage<T> message);
}

public class Consumer<T> : Initializer<T>, IConsumer<T>
{
    ILogger<Consumer<T>> logger;
    protected string consumerTag;
    public event MessageReceivedDelegate<T> MessageReceived;
    protected readonly static object locker = new object();
    protected bool disposedValue = false;

    public Consumer(IOptions<ServiceConfiguration> serviceConfigurationOptions,
                    IOptionsMonitor<QueueConfiguration> queueConfigurationOptions,
                    ILogger<Consumer<T>> logger)
            : base(serviceConfigurationOptions, 
                    queueConfigurationOptions,
                    logger)
        
    {
        this.logger = logger;
    }

    protected EventingBasicConsumer CreateEventingConsumer()
    {
        return new EventingBasicConsumer(channel);
    }

    public void StartConsumption()
    {
        var consumer = CreateEventingConsumer();

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

        consumerTag = channel.BasicConsume(queueName, false, consumer);

        logger.LogInformation($"Started Listening, Consumer<{typeof(T).Name}> - {queueConfiguration.QueueName}");
    }

    public void StopConsumption()
    {
        if(!connection.IsOpen)
            return;
            
        if(consumerTag != null)
            channel.BasicCancel(consumerTag);
            
        channel.Close();

        if (connection != null && connection.IsOpen)
            connection.Close();

        logger.LogInformation($"Stopped Listening, Consumer<{typeof(T).Name}> - {queueConfiguration.QueueName}");
    }

    public void Acknowledge(InboundMessage<T> message)
    {
        channel.BasicAck(message.DeliveryTag, false);
    }

    public void Reject(InboundMessage<T> message)
    {
        channel.BasicReject(message.DeliveryTag, false);
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