using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Text;

namespace PivotalServices.RabbitMQ.Messaging;

public interface IProducer<T> : IDisposable
{
    void Publish(OutboundMessage<T> message);
    void Close();
}

public class Producer<T> : IProducer<T>
{
    private readonly IOptions<ServiceConfiguration> serviceConfigurationOptions;
    protected QueueConfiguration queueConfiguration;
    protected DlxQueueConfiguration deadLetterQueueConfiguration;
    ILogger<Producer<T>> logger;
    protected IConnection connection;
    protected IModel channel;
    protected IBasicProperties basicProperties;
    protected IConnectionFactory factory;
    protected string exchangeName;
    protected string queueName;
    protected string[] routingKeysFromConfiguration;
    protected bool disposedValue = false;
    private bool isQueueBindingRequired;

    public Producer(IOptions<ServiceConfiguration> serviceConfigurationOptions,
                IOptionsMonitor<QueueConfiguration> queueConfigurationOptions,
                IOptionsMonitor<DlxQueueConfiguration> deadLetterQueueConfigurationOptions,
                ILogger<Producer<T>> logger)
    {
        this.serviceConfigurationOptions = serviceConfigurationOptions;
        this.queueConfiguration = queueConfigurationOptions.Get(nameof(T));
        this.deadLetterQueueConfiguration = deadLetterQueueConfigurationOptions.Get(nameof(T));
        this.logger = logger;
        routingKeysFromConfiguration = queueConfiguration.RoutingKeysCsv.Split(',');
        isQueueBindingRequired = queueConfiguration.ExchangeType != ExchangeType.Topic
                                    && queueConfiguration.ExchangeType != ExchangeType.Fanout;
        Initialize();
    }

    private void Initialize()
    {
        logger.LogInformation($"Initializing {nameof(Producer<T>)} - {queueConfiguration.QueueName}");
        logger.LogDebug($"Initializing {nameof(Producer<T>)} with Configuration- {JsonConvert.SerializeObject(queueConfiguration)}");

        factory = CreateConnectionFactory();
        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        exchangeName = $"{queueConfiguration.ExchangeName}-{queueConfiguration.ExchangeType}";
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

        if (isQueueBindingRequired)
        {
            DeclareQueue(properties);
        }

        channel.BasicQos(prefetchSize: 0,
                         prefetchCount: queueConfiguration.PrefetchCount,
                         global: false);

        basicProperties = channel.CreateBasicProperties();
        basicProperties.Expiration = (queueConfiguration.MessageExpirationInSeconds * 1000).ToString();
        basicProperties.Persistent = queueConfiguration.IsPersistent;
    }

    protected virtual void DeclareQueue(Dictionary<string, object> properties)
    {
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
    }

    private void CreateDeadLetterExchange(Dictionary<string, object> properties)
    {
        var dlxExchangeName = $"{deadLetterQueueConfiguration.ExchangeNamePrefix}-DLX";
        var dlxQueueName = $"{deadLetterQueueConfiguration.QueueNamePrefix}-DLQ";
        var dlxProperties = new Dictionary<string, object>();
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

    protected virtual IConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = serviceConfigurationOptions.Value.HostName,
            VirtualHost = serviceConfigurationOptions.Value.Vhost,
            Uri = new Uri(serviceConfigurationOptions.Value.Uri),
            UserName = serviceConfigurationOptions.Value.Username,
            Password = serviceConfigurationOptions.Value.Password,
            AutomaticRecoveryEnabled = true,
        };
    }

    public virtual void Publish(OutboundMessage<T> message)
    {
        var serializedMessage = JsonConvert.SerializeObject(message.Body);
        var messageBody = Encoding.UTF8.GetBytes(serializedMessage);

        message.RouteKeys = (message.RouteKeys == null || message.RouteKeys.Any())
                        ? (routingKeysFromConfiguration.Any()
                            ? routingKeysFromConfiguration
                            : new[] { string.Empty })
                        : message.RouteKeys;

        basicProperties.CorrelationId = message.CorrelationId;
        basicProperties.ReplyTo = message.ReplyTo;
        basicProperties.Priority = Convert.ToByte(message.Priority);

        foreach (var routingKey in message.RouteKeys)
        {
            if (isQueueBindingRequired)
            {
                channel.QueueBind(queueName, exchangeName, routingKey.Trim(), null);
            }

            channel.BasicPublish(exchangeName, routingKey.Trim(), basicProperties, messageBody);
        }
    }

    public void Close()
    {
        channel.Close();

        if (connection != null && connection.IsOpen)
            connection.Close();

        logger.LogInformation($"Closed, {nameof(Producer<T>)} - {queueConfiguration.QueueName}");
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
