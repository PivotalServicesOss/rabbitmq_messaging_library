namespace PivotalServices.RabbitMQ.Messaging;

public class OutboundMessage<T>
{
    public T Content { get; set; }
    public string[] RouteKeys { get; set; }
    public string ReplyTo { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public int Priority { get; set; }
}
