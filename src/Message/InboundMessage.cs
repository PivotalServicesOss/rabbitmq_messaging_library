namespace PivotalServices.RabbitMQ.Messaging;

    public class InboundMessage<T>
{
    public InboundMessage(T content, string replyTo = null, string correlationId = null, ulong deliveryTag = default(ulong))
    {
        Content = content;
        ReplyTo = replyTo;
        CorrelationId = correlationId;
        DeliveryTag = deliveryTag;
    }

    public T Content { get; set; }
    public string ReplyTo { get; set; }
    public string CorrelationId { get; set; }
    public ulong DeliveryTag { get; set; }
}
