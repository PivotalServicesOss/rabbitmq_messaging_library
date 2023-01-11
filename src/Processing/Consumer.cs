using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Collections.Generic;
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

    public Consumer(IOptions<ServiceConfiguration> serviceConfigurationOptions,
                    IOptionsMonitor<QueueConfiguration> queueConfigurationOptions,
                    IOptionsMonitor<DlxQueueConfiguration> deadLetterQueueConfigurationOptions,
                    ILogger<Consumer<T>> logger)
    {
        this.serviceConfigurationOptions = serviceConfigurationOptions;
        this.queueConfiguration = queueConfigurationOptions.Get(nameof(T));
        this.deadLetterQueueConfiguration = deadLetterQueueConfigurationOptions.Get(nameof(T));
        this.logger = logger;
        Initialize();
    }

    private void Initialize()
    {
        logger.LogInformation($"Initializing {nameof(Consumer<T>)} - {queueConfiguration.QueueName}");
        logger.LogDebug($"Initializing {nameof(Consumer<T>)} with Configuration- {JsonConvert.SerializeObject(queueConfiguration)}");

        factory = CreateConnectionFactory();
        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        var exchangeName = $"{queueConfiguration.ExchangeName}-{queueConfiguration.ExchangeType}";
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

        if (!queueConfiguration.RoutingKeysCsv.Split(',').Any())
        {
            channel.QueueBind(queue: queueName,
                                exchange: exchangeName,
                                routingKey: string.Empty,
                                arguments: null);
        }
        else
        {
            foreach (var routingKey in queueConfiguration.RoutingKeysCsv.Split(','))
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
                      message = new InboundMessage<T>
                      {
                          DeliveryTag = eventArgs.DeliveryTag,
                          CorrelationId = eventArgs.BasicProperties.CorrelationId,
                          ReplyTo = eventArgs.BasicProperties.ReplyTo,
                          Content = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(eventArgs.Body.ToArray()))
                      };

                      logger.LogDebug($"Received message {JsonConvert.SerializeObject(message)}");
                      logger.LogInformation($"Received a message with Correleation Id {message.CorrelationId}");

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

        logger.LogInformation($"Started Listening, {nameof(Consumer<T>)} - {queueConfiguration.QueueName}");
    }

    public void StopConsumption()
    {
        if(!connection.IsOpen)
            return;
            
        channel.BasicCancel(consumerTag);
        channel.Close();

        if (connection != null && connection.IsOpen)
            connection.Close();

        logger.LogInformation($"Stopped Listening, {nameof(Consumer<T>)} - {queueConfiguration.QueueName}");
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