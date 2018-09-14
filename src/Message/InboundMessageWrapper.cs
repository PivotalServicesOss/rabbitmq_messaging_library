namespace Messaging.Message
{
    public class InboundMessageWrapper : IInboundMessageWrapper
    {
        public string Message { get; set; }
        public string ReplyTo { get; set; }
        public string CorrelationId { get; set; }
        public ulong DeliveryTag { get; set; }
    }
}