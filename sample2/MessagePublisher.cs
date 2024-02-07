using PivotalServices.RabbitMQ.Messaging;
using Newtonsoft.Json;

namespace RabbitMQ.Sample2;

public class MessagePublisher : IHostedService
{
    private readonly IHostApplicationLifetime applicationLifetime;
    private readonly ILogger<MessagePublisher> logger;
    private readonly IProducer<Message> producer;

    public MessagePublisher(IHostApplicationLifetime applicationLifetime,
                            ILogger<MessagePublisher> logger,
                            IProducer<Message> producer)
    {
        this.applicationLifetime = applicationLifetime;
        this.logger = logger;
        this.producer = producer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var routeKeys = new[] { "my.quorum.test" };
        var text = "Hello RabbitMQ!";
        var index = 1;
        while (true)
        {
            if(applicationLifetime.ApplicationStopping.IsCancellationRequested)
                break;

            var myMessage = new Message { SomeText = $"{text} - {index}" };
            producer.Send(new OutboundMessage<Message>(myMessage, routeKeys));
            index++;
            Thread.Sleep(1000);
        }

        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
