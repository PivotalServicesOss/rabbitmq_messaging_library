using System;
using Xunit;
using Messaging.Configuration;

namespace UnitTest.Configuration
{
    public class OutboundQueueConfigurationOptionsTests
    {
        [Fact]
        public void TestOptions()
        {
            QueueConfigurationOptions options = new OutboundQueueConfigurationOptions();
            Assert.Null(options.ServiceInstanceName);
            Assert.Equal(string.Empty, options.ExchangeName);
            Assert.Equal("direct", options.ExchangeType);
            Assert.Equal(string.Empty, options.QueueName);
            Assert.Equal(string.Empty, options.RoutingKeysCsv);
            Assert.Equal(3600, options.MessageExpirationInSeconds);
            Assert.Equal(10000, options.MaximumQueueLength);
            Assert.Equal(1, options.PrefetchCount);
            Assert.Equal(false, options.IsDurable);
            Assert.Equal(false, options.IsPersistent);
            Assert.Equal(false, options.AutoDelete);
        }
    }
}
