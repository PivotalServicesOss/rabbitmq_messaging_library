namespace PivotalServices.RabbitMQ.Messaging;

public class DlxQueueConfiguration
{
    public string ExchangeNamePrefix { get; set; } = string.Empty;
    public string QueueNamePrefix { get; set; } = string.Empty;
    public int MaximumQueueLength { get; set; } = 10000;
}