namespace PivotalServices.RabbitMQ.Messaging;

public class QueueConfiguration
{
    internal bool AddDlxq{ get; set; } = false;
    internal string ExchangeName { get; set; } = string.Empty;
    public string ExchangeType { get; set; } = "direct";
    internal string QueueName { get; set; } = string.Empty;
    public string RoutingKeysCsv { get; set; } = default;
    public int MessageExpirationInSeconds { get; set; } = 3600;
    public int MaximumQueueLength { get; set; } = 10000;
    public ushort PrefetchCount { get; set; } = 1;
    public bool IsDurable { get; set; } = false;
    public bool IsPersistent { get; set; } = false;
    public bool AutoDelete { get; set; } = false;
    public bool IsExclusive { get; set; } = false;
    public int MaxPriority { get; set; } = 10;

}