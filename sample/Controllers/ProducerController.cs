using Microsoft.AspNetCore.Mvc;
using PivotalServices.RabbitMQ.Messaging;

namespace RabbitMQ.Sample;

[ApiController]
[Route("[controller]")]
public class ProducerController : ControllerBase
{
    private readonly ILogger<ProducerController> logger;
    private readonly IProducer<MyMessage> producer;

    public ProducerController(ILogger<ProducerController> logger, IProducer<MyMessage> producer, ConsumerProcessor roandom)
    {
        this.logger = logger;
        this.producer = producer;
    }

    [HttpGet("send/{text}")]
    public void Send(string text)
    {
        producer.Publish(new OutboundMessage<MyMessage>() { Body = new MyMessage { SomeText = "Hello" } });
    }
}
