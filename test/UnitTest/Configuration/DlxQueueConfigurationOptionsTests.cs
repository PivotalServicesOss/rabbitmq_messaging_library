using System;
using Xunit;
using Messaging.Configuration;

namespace UnitTest.Configuration
{
    public class DlxQueueConfigurationOptionsTests
    {
        [Fact]
        public void TestOptions()
        {
            var options = new DlxQueueConfigurationOptions();
            Assert.Equal(string.Empty, options.ExchangeNamePrefix);
            Assert.Equal(string.Empty, options.QueueNamePrefix);
            Assert.Equal(10000, options.MaximumQueueLength);
        }
    }
}
