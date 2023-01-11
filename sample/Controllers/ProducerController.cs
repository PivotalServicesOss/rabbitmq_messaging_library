using Microsoft.AspNetCore.Mvc;
using PivotalServices.RabbitMQ.Messaging;

namespace RabbitMQ.Sample;

[ApiController]
[Route("[controller]")]
public class ProducerController : ControllerBase
{
    private readonly ILogger<ProducerController> logger;
    private readonly IProducer<MyMessage> producer;

    public ProducerController(ILogger<ProducerController> logger, 
                                IProducer<MyMessage> producer)
    {
        this.logger = logger;
        this.producer = producer;
    }

    [HttpGet("send/{text}")]
    public void Send(string text)
    {
        var myMessage = new MyMessage { SomeText = text };
        producer.Send(new OutboundMessage<MyMessage>(myMessage));
    }
}