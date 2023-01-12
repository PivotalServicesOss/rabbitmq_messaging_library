using Microsoft.Extensions.DependencyInjection.Extensions;
using PivotalServices.RabbitMQ.Messaging;

namespace RabbitMQ.Sample;

public static class Program
{
    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        // Configure RabbitMQ
        // Important: Make sure the queue definitions and configurations are same between a consumer and a producer for a same queue
        builder.Services.AddRabbitMQ(cfg => {
            var exchangeName = "sample";
            var queueOneName = "queue.1";
            var queueTwoName = "queue.2";
            var addDeadLetterQueueForQueue1 = false;

            // Configure Queue 1
            cfg.AddProducer<MyMessage>(exchangeName: exchangeName,
                                       queueName: queueOneName,
                                       addDeadLetterQueue: addDeadLetterQueueForQueue1);

            cfg.AddConsumer<MyMessage>(exchangeName: exchangeName,
                                       queueName: queueOneName,
                                       addDeadLetterQueue: addDeadLetterQueueForQueue1);

            // Configure Queue 2
            cfg.AddProducer<MyMessage2>(exchangeName: exchangeName,
                                        queueName: queueTwoName);

            cfg.AddConsumer<MyMessage2>(exchangeName: exchangeName,
                                        queueName: queueTwoName);
        });

        // Add a processer to process the consumed message
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MessageProcessor>());

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}