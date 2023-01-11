using Microsoft.AspNetCore.Mvc;
using PivotalServices.RabbitMQ.Messaging;

namespace RabbitMQ.Sample;

[ApiController]
[Route("[controller]")]
public class ProducerController : ControllerBase
{
    private readonly ILogger<ProducerController> logger;
    private readonly IProducer<MyMessage> producer;
    private readonly IProducer<MyMessage2> producer2;

    public ProducerController(ILogger<ProducerController> logger,
                              IProducer<MyMessage> producer,
                              IProducer<MyMessage2> producer2)
    {
        this.logger = logger;
        this.producer = producer;
        this.producer2 = producer2;
    }

    [HttpGet("send/{text}")]
    public void Send(string text)
    {
        var myMessage = new MyMessage { SomeText = text };
        producer.Send(new OutboundMessage<MyMessage>(myMessage));
    }

    [HttpGet("send2/{text}")]
    public void Send2(string text)
    {
        var myMessage2 = new MyMessage2 { SomeText = text };
        producer2.Send(new OutboundMessage<MyMessage2>(myMessage2));
    }
}