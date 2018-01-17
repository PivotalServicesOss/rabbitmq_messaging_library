using System;
using Messaging.Message;

namespace Messaging.Queue
{
    public interface IOutboundMessageQueue : IDisposable
    {
        void Publish(IQueueMessage message, string correlationId, string replyTo, params string[] routingKeys);
        void Close();
    }
}