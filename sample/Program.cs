using Microsoft.Extensions.DependencyInjection.Extensions;
using PivotalServices.RabbitMQ.Messaging;

namespace RabbitMQ.Sample;

public static class Program
{
    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddRabbitMQ(cfg => {
            cfg.AddProducer<MyMessage>(exchangeName: "test-ex", queueName: "queue-1");
            cfg.AddProducer<MyMessage2>(exchangeName: "test-ex", queueName: "queue-2");

            cfg.AddConsumer<MyMessage>(exchangeName: "test-ex", queueName: "queue-1");
            cfg.AddConsumer<MyMessage2>(exchangeName: "test-ex", queueName: "queue-2");
        });

        // configureDlx: dlxOptions => {
        //             dlxOptions.ExchangeNamePrefix = "sample-ex";
        //             dlxOptions.ExchangeNamePrefix = "queue-1";
        //         });

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