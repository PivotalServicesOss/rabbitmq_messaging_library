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
    public class InboundMessageQueue : IInboundMessageQueue
    {
        protected IOptions<CloudFoundryServicesOptions> cfOptions;
        protected IOptions<InboundQueueConfigurationOptions> queueConfigOptions;
        protected IOptions<DlxQueueConfigurationOptions> dealLetterConfigurationOptions;
        ILogger<InboundMessageQueue> logger;
        protected Service queueBingingService;
        protected IConnection connection;
        protected IModel channel;
        protected IBasicProperties basicProperties;
        protected IConnectionFactory factory;
        protected string consumerTag;
        protected string queueName;
        public event MessageReceivedDelegate MessageReceived;
        protected readonly static object locker = new object();
        protected bool disposedValue = false;

        public InboundMessageQueue(IOptions<CloudFoundryServicesOptions> cfOptions,
                                    IOptions<InboundQueueConfigurationOptions> queueConfigOptions,
                                    IOptions<DlxQueueConfigurationOptions> dealLetterConfigurationOptions,
                                    ILogger<InboundMessageQueue> logger)
        {
            this.cfOptions = cfOptions;
            this.queueConfigOptions = queueConfigOptions;
            this.dealLetterConfigurationOptions = dealLetterConfigurationOptions;
            this.logger = logger;

            queueBingingService = cfOptions.Value.Services.FirstOrDefault(Service => Service.Name == queueConfigOptions.Value.ServiceInstanceName);

            Initialize();
        }

        private void Initialize()
        {
            logger.LogInformation($"Initializing Inbound Queue - {queueConfigOptions.Value.QueueName}");
            logger.LogDebug($"Initializing Inbound Queue with Configuration- {JsonConvert.SerializeObject(queueConfigOptions.Value)}");

            factory = CreateConnectionFactory();
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            var exchangeName = $"{queueConfigOptions.Value.ExchangeName}-{queueConfigOptions.Value.ExchangeType}";
            var properties = new Dictionary<string, object>();

            channel.ExchangeDeclare(exchangeName, queueConfigOptions.Value.ExchangeType);

            CreateDeadLetterExchange(properties);
            properties["x-max-length"] = queueConfigOptions.Value.MaximumQueueLength;

            queueName = channel.QueueDeclare(queueConfigOptions.Value.QueueName, queueConfigOptions.Value.IsDurable, false, queueConfigOptions.Value.AutoDelete, properties).QueueName;

            if (!queueConfigOptions.Value.RoutingKeysCsv.Split(',').Any())
                channel.QueueBind(queueName, exchangeName, string.Empty, null);
            else
                foreach (var routingKey in queueConfigOptions.Value.RoutingKeysCsv.Split(','))
                    channel.QueueBind(queueName, exchangeName, routingKey.Trim(), null);

            channel.BasicQos(0, queueConfigOptions.Value.PrefetchCount, false);

            basicProperties = channel.CreateBasicProperties();
            basicProperties.Expiration = (queueConfigOptions.Value.MessageExpirationInSeconds * 1000).ToString();
            basicProperties.Persistent = queueConfigOptions.Value.IsPersistent;
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

        protected virtual EventingBasicConsumer CreateEventingConsumer()
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
                      InboundMessageWrapper messageWrapper = null;

                      try
                      {
                          messageWrapper = new InboundMessageWrapper
                          {
                              DeliveryTag = eventArgs.DeliveryTag,
                              CorrelationId = eventArgs.BasicProperties.CorrelationId,
                              ReplyTo = eventArgs.BasicProperties.ReplyTo,
                              Message = Encoding.UTF8.GetString(eventArgs.Body)
                          };

                          MessageReceived?.Invoke(messageWrapper);
                      }
                      catch (Exception exception)
                      {
                          Reject(messageWrapper);
                          logger.LogError(exception, $"Failed processing message, so rejecting", messageWrapper);
                      }
                  }
              };

            consumerTag = channel.BasicConsume(queueName, false, consumer);

            logger.LogInformation($"Started Listening, Inbound Queue - {queueConfigOptions.Value.QueueName}");
        }

        public void StopConsumption()
        {
            channel.BasicCancel(consumerTag);
            channel.Close();

            if (connection != null && connection.IsOpen)
                connection.Close();

            logger.LogInformation($"Stopped Listening, Inbound Queue - {queueConfigOptions.Value.QueueName}");
        }

        public void Acknowledge(InboundMessageWrapper messageWrapper)
        {
            channel.BasicAck(messageWrapper.DeliveryTag, false);
        }

        public void Reject(InboundMessageWrapper messageWrapper)
        {
            channel.BasicReject(messageWrapper.DeliveryTag, false);
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
}