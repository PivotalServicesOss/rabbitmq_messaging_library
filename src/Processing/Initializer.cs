using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace PivotalServices.RabbitMQ.Messaging;

public abstract class Initializer<T>
{
    protected readonly IOptions<ServiceConfiguration> serviceConfigurationOptions;
    protected readonly QueueConfiguration queueConfiguration;
    protected readonly DlxQueueConfiguration deadLetterQueueConfiguration;
    ILogger logger;
    protected IConnection connection;
    protected IModel channel;
    protected IConnectionFactory factory;
    protected IBasicProperties basicProperties;
    protected List<string> routingKeysFromConfiguration;
    protected string queueName;

    public Initializer(IOptions<ServiceConfiguration> serviceConfigurationOptions,
                        IOptionsMonitor<QueueConfiguration> queueConfigurationOptions,
                        IOptionsMonitor<DlxQueueConfiguration> deadLetterQueueConfigurationOptions,
                        ILogger logger)
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
}