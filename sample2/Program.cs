using Microsoft.Extensions.DependencyInjection.Extensions;
using PivotalServices.RabbitMQ.Messaging;
using RabbitMQ.Client;

namespace RabbitMQ.Sample2;

public static class Program
{
    public static void Main(string[] args)
    {

        using var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddRabbitMQ(hostContext.Configuration, cfg =>
            {
                cfg.AddProducer<Message>(bindingExchangeName: "first-exchange",
                    queueName: "publisher-queue",
                    addDeadLetterQueue: false, (queueConfiguration) =>
                    {
                        queueConfiguration.BindingExchangeType = ExchangeType.Fanout;
                        queueConfiguration.AdditionalArguments.Add("x-queue-type", "quorum");
                    });

                cfg.AddConsumer<Queue1Message>(bindingExchangeName: "second-exchange",
                    queueName: "queue-one",
                    addDeadLetterQueue: false, (queueConfiguration) =>
                    {
                        queueConfiguration.BindingExchangeType = ExchangeType.Fanout;
                        queueConfiguration.AdditionalArguments.Add("x-queue-type", "quorum");
                    });

                cfg.AddConsumer<Queue2Message>(bindingExchangeName: "second-exchange",
                    queueName: "queue-two",
                    addDeadLetterQueue: false, (queueConfiguration) =>
                    {
                        queueConfiguration.BindingExchangeType = ExchangeType.Fanout;
                        queueConfiguration.AdditionalArguments.Add("x-queue-type", "quorum");
                    });
            });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MessageProcessor>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MessagePublisher>());

        }).Build();

        host.Start();
        host.WaitForShutdown();
    }
}