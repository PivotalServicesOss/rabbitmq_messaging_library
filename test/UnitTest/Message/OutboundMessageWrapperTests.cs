using System;
using Xunit;
using Messaging.Message;

namespace UnitTest.Message
{
    public class OutboundMessageWrapperTests
    {
        [Fact]
        public void TestMessage()
        {
            IOutboundMessageWrapper message = new OutboundMessageWrapper();
            Assert.Null(message.RouteKeys);
            Assert.Null(message.Message);

            message.Message = new QueueMessage();
            message.RouteKeys = new []{""};          
        }
    }
}