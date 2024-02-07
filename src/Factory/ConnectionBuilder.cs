using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PivotalServices.RabbitMQ.Messaging;

public interface IConnectionBuilder<T> : IDisposable
{
    IModel CurrentChannel { get; }
    string CurrentQueueName { get; }
    string CurrentExchangeType { get; }
    IBasicProperties CurrentBasicProperties { get; }
    string CurrentExchangeName { get; }
    List<string> CurrentRoutingKeys { get; }
    void InitializeConnection();
    void CloseConnection();
}

public class ConnectionBuilder<T> : IConnectionBuilder<T>
{
    protected readonly static object locker = new object();
    private readonly IOptions<ServiceConfiguration> serviceConfigurationOptions;
    private readonly IOptionsMonitor<QueueConfiguration> queueConfigurationOptions;
    private readonly ILogger<ConnectionBuilder<T>> logger;
    private QueueConfiguration queueConfiguration;
    private IConnection connection;
    private IModel channel;
    private IConnectionFactory factory;
    private IBasicProperties basicProperties;
    private List<string> routingKeysFromConfiguration;
    private string queueName;
    public IModel CurrentChannel => channel;
    public string CurrentQueueName => queueName;
    public string CurrentExchangeType => queueConfiguration.BindingExchangeType;
    public IBasicProperties CurrentBasicProperties => basicProperties;
    public string CurrentExchangeName => queueConfiguration.BindingExchangeName;
    public List<string> CurrentRoutingKeys => routingKeysFromConfiguration;
    private bool isInitialized;
    private bool isClosed;
    protected bool isDisposed;

    public ConnectionBuilder(IOptions<ServiceConfiguration> serviceConfigurationOptions,
                        IOptionsMonitor<QueueConfiguration> queueConfigurationOptions,
                        ILogger<ConnectionBuilder<T>> logger)
    {
        this.serviceConfigurationOptions = serviceConfigurationOptions;
        this.queueConfigurationOptions = queueConfigurationOptions;
        this.logger = logger;
    }

    public void InitializeConnection()
    {
        lock (locker)
        {
            if (isInitialized)
                return;

            var optionsName = typeof(T).Name;
            queueConfiguration = queueConfigurationOptions.Get(optionsName);

            logger.LogInformation("Initializing connection to Queue[{queue}], Exchange[{exchange}], for Message[{message}]",
                                        queueConfiguration.QueueName,
                                        queueConfiguration.BindingExchangeName,
                                        typeof(T).Name);

            routingKeysFromConfiguration = queueConfiguration.RoutingKeysCsv == default
                                                ? new List<string>(){ string.Empty}
                                                : queueConfiguration.RoutingKeysCsv.Split(',').ToList();

            factory = CreateConnectionFactory();
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            var exchangeName = queueConfiguration.BindingExchangeName;
            var properties = queueConfiguration.AdditionalArguments;

            channel.ExchangeDeclare(exchange: exchangeName,
                                    type: queueConfiguration.BindingExchangeType);

            if (queueConfiguration.ConfigureDeadLetterQueue)
            {
                CreateDeadLetterExchange(properties);
            }

            queueName = channel.QueueDeclare(queue: queueConfiguration.QueueName,
                                            durable: queueConfiguration.IsDurable,
                                            exclusive: queueConfiguration.IsExclusive,
                                            autoDelete: queueConfiguration.AutoDelete,
                                            arguments: properties).QueueName;


            foreach (var routingKey in routingKeysFromConfiguration)
            {
                channel.QueueBind(queue: queueName,
                                    exchange: exchangeName,
                                    routingKey: routingKey.Trim(),
                                    arguments: null);
            }

            channel.BasicQos(0, queueConfiguration.PrefetchCount, false);

            basicProperties = channel.CreateBasicProperties();

            if(queueConfiguration.MessageExpirationInSeconds > default(int))
                basicProperties.Expiration = (queueConfiguration.MessageExpirationInSeconds * 1000).ToString();

            basicProperties.Persistent = queueConfiguration.IsPersistent;
            isInitialized = true;
        }
    }

    public void CloseConnection()
    {
        lock (locker)
        {
            if (isClosed 
                || !isInitialized
                || connection == null
                || !connection.IsOpen)
                return;

            channel?.Close();

            if (connection.IsOpen)
                connection.Close();

            logger.LogInformation("Closed connection to Queue[{queue}], Exchange[{exchange}], for Message[{message}]",
                                        queueConfiguration.QueueName,
                                        queueConfiguration.BindingExchangeName,
                                        typeof(T).Name);
            isClosed = true;
        }
    }

    private void CreateDeadLetterExchange(Dictionary<string, object> properties)
    {
        var dlxExchangeName = $"{queueConfiguration.BindingExchangeName}-DLX";
        var dlxQueueName = $"{queueConfiguration.QueueName}-DLQ";
        var dlxProperties = new Dictionary<string, object>();

        channel.ExchangeDeclare(exchange: dlxExchangeName, type: ExchangeType.Direct);
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

    private IConnectionFactory CreateConnectionFactory()
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

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
                CloseConnection();

            isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}