namespace PivotalServices.RabbitMQ.Messaging;

public class QueueConfiguration
{
    internal bool ConfigureDeadLetterQueue{ get; set; } = false;
    internal string BindingExchangeName { get; set; } = string.Empty;
    public string BindingExchangeType { get; set; } = "direct";
    internal string QueueName { get; set; } = string.Empty;
    public string RoutingKeysCsv { get; set; } = null;
    public int MessageExpirationInSeconds { get; set; } = default;
    public ushort PrefetchCount { get; set; } = 1;
    public bool IsDurable { get; set; } = true;
    public bool IsPersistent { get; set; } = false;
    public bool AutoDelete { get; set; } = false;
    public bool IsExclusive { get; set; } = false;
    public Dictionary<string, object> AdditionalArguments { get; set; } = new Dictionary<string, object>();
}