using System;
using Xunit;
using FluentAssertions;

namespace Unit.Test
{
    public class QueueConfigurationTests
    {
        [Fact]
        public void ShouldSetDefaults()
        {
            var config = new QueueConfiguration();
            config.AddDeadLetterQueue.Should().BeTrue();
            config.ExchangeName.Should().BeEmpty();
            config.ExchangeType.Should().Be("direct");
            config.QueueName.Should().BeEmpty();
            config.RoutingKeysCsv.Should().BeNull();
            config.MessageExpirationInSeconds.Should().Be(3600);
            config.MaximumQueueLength.Should().Be(10000);
            config.PrefetchCount.Should().Be(1);
            config.IsPersistent.Should().BeFalse();
            config.IsDurable.Should().BeFalse();
            config.AutoDelete.Should().BeFalse();
            config.IsExclusive.Should().BeFalse();
            config.MaxPriority.Should().Be(10);
        }
    }
}
