using System;
using Xunit;
using Messaging.Message;

namespace UnitTest.Message
{
    public class QueueMessageTests
    {
        [Fact]
        public void TestMessage()
        {
            IQueueMessage message = new QueueMessage();
            Assert.Null(message.Key);
            Assert.Null(message.Body);
        }
    }
}