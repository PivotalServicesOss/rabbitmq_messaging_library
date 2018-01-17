using Messaging.Message;

namespace Messaging.Queue
{
    public interface IOutboundMessageQueue
    {
        void Publish(IQueueMessage message, string correlationId, string replyTo, params string[] routingKeys);
    }
}