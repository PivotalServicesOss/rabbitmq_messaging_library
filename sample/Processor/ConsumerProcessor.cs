using PivotalServices.RabbitMQ.Messaging;
using System.Diagnostics;
using Newtonsoft.Json;

namespace RabbitMQ.Sample;

public class ConsumerProcessor
{
    private readonly IConsumer<MyMessage> consumer;
    private readonly ILogger<ConsumerProcessor> logger;
    Stopwatch stopWatch = new Stopwatch();

    public ConsumerProcessor(IConsumer<MyMessage> consumer, ILogger<ConsumerProcessor> logger)
    {
        this.consumer = consumer;
        this.logger = logger;
        consumer.MessageReceived += Received;
    }

    private void Received(InboundMessage<MyMessage> message)
    {
        stopWatch.Restart();
        try
        {
            using (logger.BeginScope($"{message.CorrelationId}"))
            {
                //var myMessage = JsonConvert.DeserializeObject<MyMessage>(message.Body);
                logger.LogDebug("Begin processing of message");
                
                //DoProcessing(myMessage);

                consumer.Acknowledge(message);

                logger.LogDebug($"Finished processing of message");
            }
        }
        catch (Exception exception)
        {
            consumer.Reject(message);
            logger.LogError(exception, $"Failed processing message, so rejecting", message);
        }
        finally
        {
            stopWatch.Stop();
            logger.LogInformation($"Processing time: {stopWatch.ElapsedMilliseconds} ms");
        }
    }
}