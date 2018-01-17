namespace Messaging.Message
{
    public class OutboundMessageWrapper : IOutboundMessageWrapper
    {
        public IQueueMessage Message { get; set; }
        public string[] RouteKeys { get; set; }
    }
}