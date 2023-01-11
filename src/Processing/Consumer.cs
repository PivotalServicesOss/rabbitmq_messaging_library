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

public class Consumer<T> : IConsumer<T>
{
    private readonly IOptions<ServiceConfiguration> serviceConfigurationOptions;
    protected QueueConfiguration queueConfiguration;
    protected DlxQueueConfiguration deadLetterQueueConfiguration;
    ILogger<Consumer<T>> logger;
    protected IConnection connection;
    protected IModel channel;
    protected IBasicProperties basicProperties;
    protected IConnectionFactory factory;
    protected string consumerTag;
    protected string queueName;
    public event MessageReceivedDelegate<T> MessageReceived;
    protected readonly static object locker = new object();
    protected bool disposedValue = false;
    protected List<string> routingKeysFromConfiguration;

    public Consumer(IOptions<ServiceConfiguration> serviceConfigurationOptions,
                    IOptionsMonitor<QueueConfiguration> queueConfigurationOptions,
                    IOptionsMonitor<DlxQueueConfiguration> deadLetterQueueConfigurationOptions,
                    ILogger<Consumer<T>> logger)
    {
        var optionsName = typeof(T).Name;
        this.serviceConfigurationOptions = serviceConfigurationOptions;
        this.queueConfiguration = queueConfigurationOptions.Get(optionsName);
        routingKeysFromConfiguration = queueConfiguration.RoutingKeysCsv == null 
                                            ? new List<string>() 
                                            : queueConfiguration.RoutingKeysCsv.Split(',').ToList();
        this.deadLetterQueueConfiguration = deadLetterQueueConfigurationOptions.Get(optionsName);
        this.logger = logger;
        Initialize();
    }

    private void Initialize()
    {
        logger.LogInformation($"Initializing Consumer<{typeof(T).Name}> - {queueConfiguration.QueueName}");
        logger.LogDebug($"Initializing Consumer<{typeof(T).Name}> with Configuration- {JsonConvert.SerializeObject(queueConfiguration)}");

        factory = CreateConnectionFactory();
        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        var exchangeName = queueConfiguration.ExchangeName;
        var properties = new Dictionary<string, object>();

        channel.ExchangeDeclare(exchange: exchangeName,
                                type: queueConfiguration.ExchangeType);

        if (!string.IsNullOrEmpty(deadLetterQueueConfiguration.ExchangeNamePrefix)
            && !string.IsNullOrEmpty(deadLetterQueueConfiguration.QueueNamePrefix))
        {
            CreateDeadLetterExchange(properties);
        }

        properties["x-max-length"] = queueConfiguration.MaximumQueueLength;
        properties["x-max-priority"] = queueConfiguration.MaxPriority;
        queueName = channel.QueueDeclare(queue: queueConfiguration.QueueName,
                                         durable: queueConfiguration.IsDurable,
                                         exclusive: queueConfiguration.IsExclusive,
                                         autoDelete: queueConfiguration.AutoDelete,
                                         arguments: properties
                                         ).QueueName;

        if (!routingKeysFromConfiguration.Any())
        {
            channel.QueueBind(queue: queueName,
                                exchange: exchangeName,
                                routingKey: string.Empty,
                                arguments: null);
        }
        else
        {
            foreach (var routingKey in routingKeysFromConfiguration)
            {
                channel.QueueBind(queue: queueName,
                                    exchange: exchangeName,
                                    routingKey: routingKey.Trim(),
                                    arguments: null);
            }
        }

        channel.BasicQos(0, queueConfiguration.PrefetchCount, false);

        basicProperties = channel.CreateBasicProperties();
        basicProperties.Expiration = (queueConfiguration.MessageExpirationInSeconds * 1000).ToString();
        basicProperties.Persistent = queueConfiguration.IsPersistent;
    }

    private void CreateDeadLetterExchange(Dictionary<string, object> properties)
    {
        var dlxExchangeName = $"{deadLetterQueueConfiguration.ExchangeNamePrefix}-DLX";
        var dlxQueueName = $"{deadLetterQueueConfiguration.QueueNamePrefix}-DLQ";
        var dlxProperties = new Dictionary<string, object>();

        channel.ExchangeDeclare(exchange: dlxExchangeName, type: ExchangeType.Fanout);
        dlxProperties["x-max-length"] = deadLetterQueueConfiguration.MaximumQueueLength;
        channel.QueueDeclare(queue: dlxQueueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: dlxProperties);
        channel.QueueBind(queue: dlxQueueName,
                          exchange: dlxExchangeName,
                          routingKey: string.Empty,
                          arguments: null);
        properties["x-dead-letter-exchange"] = dlxExchangeName;
    }

    protected IConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = serviceConfigurationOptions.Value.HostName,
            VirtualHost = serviceConfigurationOptions.Value.Vhost,
            UserName = serviceConfigurationOptions.Value.Username,
            Password = serviceConfigurationOptions.Value.Password,
            AutomaticRecoveryEnabled = true,
        };
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