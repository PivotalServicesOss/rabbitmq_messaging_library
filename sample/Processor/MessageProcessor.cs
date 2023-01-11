using PivotalServices.RabbitMQ.Messaging;
using Newtonsoft.Json;

namespace RabbitMQ.Sample;

public class MessageProcessor : IHostedService
{
    private readonly ILogger<MessageProcessor> logger;
    private readonly IConsumer<MyMessage> consumer;

    public MessageProcessor(ILogger<MessageProcessor> logger, 
                                IConsumer<MyMessage> consumer)
    {
        this.logger = logger;
        this.consumer = consumer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        consumer.MessageReceived += Received;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        consumer.MessageReceived -= Received;
        return Task.CompletedTask;
    }

    private void Received(InboundMessage<MyMessage> message)
    {
        try
        {
            using (logger.BeginScope($"{message.CorrelationId}"))
            {
                logger.LogDebug("Begin processing of message");
                
                //Do Processing of message content here
                logger.LogInformation($"Received message content {JsonConvert.SerializeObject(message.Content)}");

                consumer.Acknowledge(message);// you can also configure auto acknowledge if needed

                logger.LogDebug($"Finished processing of message");
            }
        }
        catch (Exception exception)
        {
            consumer.Reject(message);
            logger.LogError(exception, $"Failed processing message, so rejecting", message);
        }
    }
}
