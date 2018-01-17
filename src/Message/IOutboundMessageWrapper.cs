namespace Messaging.Message
{
    public interface IOutboundMessageWrapper
    {
        IQueueMessage Message { get; set; }
        string[] RouteKeys { get; set; }
    }
}