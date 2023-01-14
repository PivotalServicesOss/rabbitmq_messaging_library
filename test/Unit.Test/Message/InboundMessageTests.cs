using System;
using Xunit;
using FluentAssertions;

namespace Unit.Test
{
    public class InboundMessageTests
    {
        private const string Content = "content";
        private const string ReplyTo = "reply2";
        private const string CorrelationId = "correlationId";
        private const int DeliveryTag = 12;

        [Fact]
        public void Ctor_ShouldSetProperties()
        {
            var message = new InboundMessage<string>(content: Content,
                                                     replyTo: ReplyTo,
                                                     correlationId: CorrelationId,
                                                     deliveryTag: DeliveryTag);
            message.Content.Should().Be(Content);
            message.ReplyTo.Should().Be(ReplyTo);
            message.CorrelationId.Should().Be(CorrelationId);
            message.DeliveryTag.Should().Be(DeliveryTag);
        }
    }
}
