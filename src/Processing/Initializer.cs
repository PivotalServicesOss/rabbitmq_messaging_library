using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PivotalServices.RabbitMQ.Messaging;

public abstract class Initializer<T>
{
    protected readonly IOptions<ServiceConfiguration> serviceConfigurationOptions;
    protected readonly QueueConfiguration queueConfiguration;
    ILogger logger;
    protected IConnection connection;
    protected IModel channel;
    protected IConnectionFactory factory;
    protected IBasicProperties basicProperties;
    protected List<string> routingKeysFromConfiguration;
    protected string queueName;

    public Initializer(IOptions<ServiceConfiguration> serviceConfigurationOptions,
                        IOptionsMonitor<QueueConfiguration> queueConfigurationOptions,
                        ILogger logger)
    {
        var optionsName = typeof(T).Name;
        this.serviceConfigurationOptions = serviceConfigurationOptions;
        this.queueConfiguration = queueConfigurationOptions.Get(optionsName);
        routingKeysFromConfiguration = queueConfiguration.RoutingKeysCsv == null
                                            ? new List<string>()
                                            : queueConfiguration.RoutingKeysCsv.Split(',').ToList();
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

        if (queueConfiguration.AddDeadLetterQueue)
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


        foreach (var routingKey in routingKeysFromConfiguration)
        {
            channel.QueueBind(queue: queueName,
                                exchange: exchangeName,
                                routingKey: routingKey.Trim(),
                                arguments: null);
        }

        channel.BasicQos(0, queueConfiguration.PrefetchCount, false);

        basicProperties = channel.CreateBasicProperties();
        basicProperties.Expiration = (queueConfiguration.MessageExpirationInSeconds * 1000).ToString();
        basicProperties.Persistent = queueConfiguration.IsPersistent;
    }

    private void CreateDeadLetterExchange(Dictionary<string, object> properties)
    {
        var dlxExchangeName = $"{queueConfiguration.ExchangeName}-DLX";
        var dlxQueueName = $"{queueConfiguration.QueueName}-DLQ";
        var dlxProperties = new Dictionary<string, object>();

        channel.ExchangeDeclare(exchange: dlxExchangeName, type: ExchangeType.Direct);
        dlxProperties["x-max-length"] = queueConfiguration.MaximumQueueLength;
        channel.QueueDeclare(queue: dlxQueueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: dlxProperties);
        channel.QueueBind(queue: dlxQueueName,
                          exchange: dlxExchangeName,
                          routingKey: dlxQueueName,
                          arguments: null);
        properties["x-dead-letter-routing-key"] = dlxQueueName;
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