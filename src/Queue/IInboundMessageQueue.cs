using System;
using Messaging.Message;

namespace Messaging.Queue
{
    public delegate void MessageReceivedDelegate(InboundMessageWrapper messageWrapper);
    public interface IInboundMessageQueue : IDisposable
    {
        event MessageReceivedDelegate MessageReceivedEvent;
        void StartConsumption();
        void StopConsumption();
        void Acknowledge(InboundMessageWrapper messageWrapper);
        void Reject(InboundMessageWrapper messageWrapper);
    }
}