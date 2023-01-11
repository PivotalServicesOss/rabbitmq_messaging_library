namespace PivotalServices.RabbitMQ.Messaging;

    public class InboundMessage<T>
{
    public T Content { get; set; }
    public string ReplyTo { get; set; }
    public string CorrelationId { get; set; }
    public ulong DeliveryTag { get; set; }
}
