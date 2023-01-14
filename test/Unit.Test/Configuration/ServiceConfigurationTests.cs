using System;
using Xunit;
using FluentAssertions;

namespace Unit.Test
{
    public class ServiceConfigurationTests
    {
        [Fact]
        public void ShouldSetDefaults()
        {
            var config = new ServiceConfiguration();
            config.HostName.Should().Be("localhost");
            config.Vhost.Should().Be("/");
            config.Username.Should().Be("guest");
            config.Password.Should().Be("guest");
        }

        [Fact]
        public void ConfigRoot_ShouldBe()
        {
            ServiceConfiguration.ConfigRoot.Should().Be("RabbitMq");
        }
    }
}
