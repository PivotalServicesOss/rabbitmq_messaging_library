namespace PivotalServices.RabbitMQ.Messaging;

public class OutboundMessage<T>
{
    public T Body { get; set; }
    public string[] RouteKeys { get; set; }
    public string ReplyTo { get; set; }
    public string CorrelationId { get; set; }
    public int Priority { get; set; }
}
