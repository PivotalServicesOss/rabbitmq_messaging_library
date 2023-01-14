using System;
using Xunit;
using FluentAssertions;

namespace Unit.Test
{
    public class GlobalContainerTests
    {
        public GlobalContainerTests()
        {
            Global.ConsumerTypes.Add(typeof(string));
            Global.ConsumerTypes.Add(typeof(int));
            Global.ProducerTypes.Add(typeof(decimal));
            Global.ProducerTypes.Add(typeof(float));
            Global.ProducerTypes.Add(typeof(bool));
        }

        [Fact]
        public void CheckProperties()
        {
            Global.ConsumerTypes.Should().HaveCount(2);
            Global.ProducerTypes.Should().HaveCount(3);

            Global.ConsumerTypes.Should().Contain(typeof(string));
            Global.ConsumerTypes.Should().Contain(typeof(int));
            Global.ProducerTypes.Should().Contain(typeof(decimal));
            Global.ProducerTypes.Should().Contain(typeof(float));
            Global.ProducerTypes.Should().Contain(typeof(bool));

        }
    }
}
