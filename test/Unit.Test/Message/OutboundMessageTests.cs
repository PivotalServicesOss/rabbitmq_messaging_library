using System;
using Xunit;
using FluentAssertions;

namespace Unit.Test
{
    public class OutboundMessageTests
    {
        private const string Content = "content";
        private const string ReplyTo = "reply2";
        private const string CorrelationId = "correlationId";
        private const int Priority = 12;
        private string[] RouteKeys = new[] { "route-key" };

        [Fact]
        public void Ctor_ShouldSetProperties()
        {
            var message = new OutboundMessage<string>(content: Content,
                                                     routeKeys: RouteKeys,
                                                     replyTo: ReplyTo,
                                                     correlationId: CorrelationId,
                                                     priority: Priority);
            message.Content.Should().Be(Content);
            message.RouteKeys.Should().BeSameAs(RouteKeys);
            message.ReplyTo.Should().Be(ReplyTo);
            message.CorrelationId.Should().Be(CorrelationId);
            message.Priority.Should().Be(Priority);
        }

        [Fact]
        public void Creates_CorrelationId_IfNotSet()
        {
            var message = new OutboundMessage<string>(content: Content);
            message.CorrelationId.Should().NotBeNullOrEmpty();
            message.CorrelationId.Should().MatchRegex(@"[a-fA-F\d]{8}-[a-fA-F\d]{4}-[a-fA-F\d]{4}-[a-fA-F\d]{4}-[a-fA-F\d]{12}");
        }
    }
}
