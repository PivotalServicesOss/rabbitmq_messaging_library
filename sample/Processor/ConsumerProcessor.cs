using PivotalServices.RabbitMQ.Messaging;

namespace RabbitMQ.Sample;

public class ConsumerProcessor
{
    private readonly IConsumer<MyMessage> consumer;
    private readonly ILogger<ConsumerProcessor> logger;

    public ConsumerProcessor(IConsumer<MyMessage> consumer, ILogger<ConsumerProcessor> logger)
    {
        this.consumer = consumer;
        this.logger = logger;
        consumer.MessageReceived+=Received;
    }

    private void Received(InboundMessage<MyMessage> message)
    {
        logger.LogInformation($"Message received: {message.Body}");
    }
}