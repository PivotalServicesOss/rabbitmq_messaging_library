using System;
using Xunit;
using Messaging.Message;

namespace UnitTest.Message
{
    public class InboundMessageWrapperTests
    {
        [Fact]
        public void TestMessage()
        {
            IInboundMessageWrapper message = new InboundMessageWrapper();
            Assert.Null(message.CorrelationId);
            Assert.Null(message.ReplyTo);
            Assert.Null(message.Message);
            Assert.Equal(default(ulong), message.DeliveryTag);
        }
    }
}