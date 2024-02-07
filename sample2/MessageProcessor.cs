using PivotalServices.RabbitMQ.Messaging;
using Newtonsoft.Json;

namespace RabbitMQ.Sample2;

public class MessageProcessor : IHostedService
{
    private readonly IHostApplicationLifetime applicationLifetime;
    private readonly ILogger<MessageProcessor> logger;
    private readonly IConsumer<Queue1Message> queue1Consumer;
    private readonly IConsumer<Queue2Message> queue2Consumer;

    public MessageProcessor(IHostApplicationLifetime applicationLifetime,
                            ILogger<MessageProcessor> logger,
                            IConsumer<Queue1Message> queue1Consumer,
                            IConsumer<Queue2Message> queue2Consumer)
    {
        this.applicationLifetime = applicationLifetime;
        this.logger = logger;
        this.queue1Consumer = queue1Consumer;
        this.queue2Consumer = queue2Consumer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        queue1Consumer.MessageReceived += OnReceived_Queue1;
        queue2Consumer.MessageReceived += OnReceived_Queue2;
        applicationLifetime.ApplicationStarted.Register(() =>
        {
            StopAsync(applicationLifetime.ApplicationStopping);
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        queue1Consumer.MessageReceived -= OnReceived_Queue1;
        queue2Consumer.MessageReceived -= OnReceived_Queue2;
        return Task.CompletedTask;
    }

    private void OnReceived_Queue1(InboundMessage<Queue1Message> message)
    {
        try
        {
                logger.LogInformation($"Received from Queue1 {JsonConvert.SerializeObject(message.Content)}");
                queue1Consumer.Acknowledge(message);
        }
        catch (Exception exception)
        {
            queue1Consumer.Reject(message);
            logger.LogError(exception, $"Failed processing message from Queue1, so rejecting", message);
        }
    }

    private void OnReceived_Queue2(InboundMessage<Queue2Message> message)
    {
        try
        {
            {
                logger.LogInformation($"Received from Queue2 {JsonConvert.SerializeObject(message.Content)}");
                queue2Consumer.Acknowledge(message);
            }
        }
        catch (Exception exception)
        {
            queue2Consumer.Reject(message);
            logger.LogError(exception, $"Failed processing message from Queue2, so rejecting", message);
        }
    }
}
