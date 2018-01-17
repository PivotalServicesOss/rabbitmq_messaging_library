namespace Messaging.Message
{
    public interface IInboundMessageWrapper
    {
        string Message { get; set; }
        string ReplyTo { get; set; }
        string CorrelationId { get; set; }
        ulong DeliveryTag { get; set; }
    }
}