namespace Unit.Test
{
    public class QueueConfigurationTests
    {
        [Fact]
        public void ShouldSetDefaults()
        {
            var config = new QueueConfiguration();
            config.ConfigureDeadLetterQueue.Should().BeFalse();
            config.BindingExchangeName.Should().BeEmpty();
            config.BindingExchangeType.Should().Be("direct");
            config.QueueName.Should().BeEmpty();
            config.RoutingKeysCsv.Should().BeNull();
            config.MessageExpirationInSeconds.Should().Be(default(int));
            config.PrefetchCount.Should().Be(1);
            config.IsPersistent.Should().BeFalse();
            config.IsDurable.Should().BeTrue();
            config.AutoDelete.Should().BeFalse();
            config.IsExclusive.Should().BeFalse();
        }
    }
}
