using System;
using Xunit;
using FluentAssertions;

namespace Unit.Test
{
    public class InboundMessageTests
    {
        [Fact]
        public void Ctor_ShouldSetDefaults()
        {
            var message = new InboundMessage<string>("");
        }
    }
}
