using RabbitMQ.Client;

namespace Messaging.Configuration
{
    public abstract class QueueConfigurationOptions
    {
        public string ServiceInstanceName{ get; set; }
        public string ExchangeName{ get; set; } = string.Empty;
        public string ExchangeType{ get; set; } = "direct";
        public string QueueName{ get; set; } = string.Empty;
        public string RoutingKeysCsv{ get; set; } = string.Empty;
        public int MessageExpirationInSeconds{ get; set; } = 3600;
        public int MaximumQueueLength{ get; set; } = 10000;
        public ushort PrefetchCount{ get; set; } = 1;
        public bool IsDurable{ get; set; } = false;
        public bool IsPersistent{ get; set; } = false;
        public bool AutoDelete{ get; set; } = false;
    }
}