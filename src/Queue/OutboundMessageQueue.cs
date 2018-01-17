using Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System.Linq;
using System;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Collections.Generic;
using Messaging.Message;
using RabbitMQ.Client.Events;
using System.Text;

namespace Messaging.Queue
{
    public class OutboundMessageQueue : IOutboundMessageQueue
    {
        protected IOptions<CloudFoundryServicesOptions> cfOptions;
        protected IOptions<OutboundQueueConfigurationOptions> queueConfigOptions;
        protected IOptions<DlxQueueConfigurationOptions> dealLetterConfigurationOptions;
        ILogger<OutboundMessageQueue> logger;
        protected Service queueBingingService;
        protected IConnection connection;
        protected IModel channel;
        protected IBasicProperties basicProperties;
        protected IConnectionFactory factory;
        protected string exchangeName;
        protected string queueName;
        protected string[] routingKeysFromConfiguration;
        protected bool disposedValue = false;
        private bool isQueueBindingRequired;

        public OutboundMessageQueue(IOptions<CloudFoundryServicesOptions> cfOptions,
                                    IOptions<OutboundQueueConfigurationOptions> queueConfigOptions,
                                    IOptions<DlxQueueConfigurationOptions> dealLetterConfigurationOptions,
                                    ILogger<OutboundMessageQueue> logger)
        {
            this.cfOptions = cfOptions;
            this.queueConfigOptions = queueConfigOptions;
            this.dealLetterConfigurationOptions = dealLetterConfigurationOptions;
            this.logger = logger;

            queueBingingService = cfOptions.Value.Services.FirstOrDefault(Service => Service.Name == queueConfigOptions.Value.ServiceInstanceName);
            routingKeysFromConfiguration = queueConfigOptions.Value.RoutingKeysCsv.Split(',');

            isQueueBindingRequired = queueConfigOptions.Value.ExchangeType != ExchangeType.Topic && queueConfigOptions.Value.ExchangeType != ExchangeType.Fanout;

            Initialize();
        }

        private void Initialize()
        {
            logger.LogInformation($"Initializing Outbound Queue - {queueConfigOptions.Value.QueueName}");
            logger.LogDebug($"Initializing Outbound Queue with Configuration- {JsonConvert.SerializeObject(queueConfigOptions.Value)}");

            factory = CreateConnectionFactory();
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            exchangeName = $"{queueConfigOptions.Value.ExchangeName}-{queueConfigOptions.Value.ExchangeType}";
            var properties = new Dictionary<string, object>();

            channel.ExchangeDeclare(exchangeName, queueConfigOptions.Value.ExchangeType);

            CreateDeadLetterExchange(properties);
            properties["x-max-length"] = queueConfigOptions.Value.MaximumQueueLength;

            if (isQueueBindingRequired)
                DeclareQueue(properties);

            channel.BasicQos(0, queueConfigOptions.Value.PrefetchCount, false);

            basicProperties = channel.CreateBasicProperties();
            basicProperties.Expiration = (queueConfigOptions.Value.MessageExpirationInSeconds * 1000).ToString();
            basicProperties.Persistent = queueConfigOptions.Value.IsPersistent;
        }

        protected virtual void DeclareQueue(Dictionary<string, object> properties)
        {
            queueName = channel.QueueDeclare(queueConfigOptions.Value.QueueName, queueConfigOptions.Value.IsDurable, false, queueConfigOptions.Value.AutoDelete, properties).QueueName;

            if (!queueConfigOptions.Value.RoutingKeysCsv.Split(',').Any())
                channel.QueueBind(queueName, exchangeName, string.Empty, null);
            else
                foreach (var routingKey in queueConfigOptions.Value.RoutingKeysCsv.Split(','))
                    channel.QueueBind(queueName, exchangeName, routingKey.Trim(), null);
        }

        private void CreateDeadLetterExchange(Dictionary<string, object> properties)
        {
            var dlxExchangeName = $"{dealLetterConfigurationOptions.Value.ExchangeNamePrefix}-DLX";
            var dlxQueueName = $"{dealLetterConfigurationOptions.Value.QueueNamePrefix}-DLQ";
            var dlxProperties = new Dictionary<string, object>();

            channel.ExchangeDeclare(dlxExchangeName, ExchangeType.Fanout);
            dlxProperties["x-max-length"] = dealLetterConfigurationOptions.Value.MaximumQueueLength;
            channel.QueueDeclare(dlxQueueName, false, false, false, dlxProperties);
            channel.QueueBind(dlxQueueName, dlxExchangeName, string.Empty, null);
            properties["x-dead-letter-exchange"] = dlxExchangeName;
        }

        protected virtual IConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory
            {
                HostName = queueBingingService.Credentials["hostname"].Value,
                VirtualHost = queueBingingService.Credentials["vhost"].Value,
                Uri = new Uri(queueBingingService.Credentials["uri"].Value),
                UserName = queueBingingService.Credentials["username"].Value,
                Password = queueBingingService.Credentials["password"].Value,
                AutomaticRecoveryEnabled = true,
            };
        }

        public virtual void Publish(IQueueMessage message, string correlationId, string replyTo, params string[] routingKeys)
        {
            var serializedMessage = JsonConvert.SerializeObject(message);
            var messageBody = Encoding.UTF8.GetBytes(serializedMessage);

            routingKeys = (routingKeys == null || routingKeys.Any())
                            ? (routingKeysFromConfiguration.Any()
                                ? routingKeysFromConfiguration
                                : new[] { string.Empty })
                            : routingKeys;

            basicProperties.CorrelationId = correlationId;
            basicProperties.ReplyTo = replyTo;

            foreach (var routingKey in routingKeys)
            {
                if (isQueueBindingRequired)
                    channel.QueueBind(queueName, exchangeName, routingKey.Trim(), null);

                channel.BasicPublish(exchangeName, routingKey.Trim(), basicProperties, messageBody);
            }
        }

        public void Close()
        {
            channel.Close();

            if (connection != null && connection.IsOpen)
                connection.Close();

            logger.LogInformation($"Closed, Outbound Queue - {queueConfigOptions.Value.QueueName}");
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
}