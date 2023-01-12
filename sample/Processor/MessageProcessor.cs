using PivotalServices.RabbitMQ.Messaging;
using Newtonsoft.Json;

namespace RabbitMQ.Sample;

public class MessageProcessor : IHostedService
{
    private readonly ILogger<MessageProcessor> logger;
    private readonly IConsumer<MyMessage> consumer;
    private readonly IConsumer<MyMessage2> consumer2;

    public MessageProcessor(ILogger<MessageProcessor> logger,
                            IConsumer<MyMessage> consumer,
                            IConsumer<MyMessage2> consumer2)
    {
        this.logger = logger;
        this.consumer = consumer;
        this.consumer2 = consumer2;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        consumer.MessageReceived += Received;
        consumer2.MessageReceived += Received2;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        consumer.MessageReceived -= Received;
        consumer2.MessageReceived -= Received2;
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
                
                if(message.Content.SomeText == "reject")
                    throw new Exception("force rejected");
                    
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

    private void Received2(InboundMessage<MyMessage2> message)
    {
        try
        {
            using (logger.BeginScope($"{message.CorrelationId}"))
            {
                logger.LogDebug("Begin processing of message2");
                
                //Do Processing of message content here
                logger.LogInformation($"Received message2 content {JsonConvert.SerializeObject(message.Content)}");

                if(message.Content.SomeText == "reject")
                    throw new Exception("force rejected");

                consumer2.Acknowledge(message);// you can also configure auto acknowledge if needed

                logger.LogDebug($"Finished processing of message2");
            }
        }
        catch (Exception exception)
        {
            consumer2.Reject(message);
            logger.LogError(exception, $"Failed processing message2, so rejecting", message);
        }
    }
}
