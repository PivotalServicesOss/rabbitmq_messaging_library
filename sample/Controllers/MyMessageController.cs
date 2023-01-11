using Microsoft.AspNetCore.Mvc;
using PivotalServices.RabbitMQ.Messaging;
using Newtonsoft.Json;

namespace RabbitMQ.Sample;

[ApiController]
[Route("[controller]")]
public class MyMessageController : ControllerBase
{
    private readonly ILogger<MyMessageController> logger;
    private readonly IProducer<MyMessage> producer;
    private readonly IConsumer<MyMessage> consumer;

    public MyMessageController(ILogger<MyMessageController> logger, 
                                IProducer<MyMessage> producer, 
                                IConsumer<MyMessage> consumer)
    {
        this.logger = logger;
        this.producer = producer;
        this.consumer = consumer;
        consumer.MessageReceived += Received;
    }

    [HttpGet("send/{text}")]
    public void Send(string text)
    {
        producer.Send(new OutboundMessage<MyMessage>() { Content = new MyMessage { SomeText = text } });
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
