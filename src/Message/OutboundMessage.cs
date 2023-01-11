namespace PivotalServices.RabbitMQ.Messaging;

public class OutboundMessage<T>
{
    public OutboundMessage(T content, string[] routeKeys = null, string replyTo = null, string correlationId = null, int priority = 0)
    {
        Content = content;
        RouteKeys = routeKeys;
        ReplyTo = replyTo;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        Priority = priority;
    }

    public T Content { get; set; }
    public string[] RouteKeys { get; set; }
    public string ReplyTo { get; set; }
    public string CorrelationId { get; set; }
    public int Priority { get; set; }
}
