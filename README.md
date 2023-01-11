### A simple messaging library that uses RabbitMQ

#### Kindly raise any issues at [Project Issues](https://github.com/alfusinigoj/rabbitmq_messaging_library/issues)

Build | PivotalServices.RabbitMQ.Messaging |
--- | --- |
[![Nuget (Prod Release)](https://github.com/alfusinigoj/rabbitmq_messaging_library/actions/workflows/prod-release-pipeline.yml/badge.svg)](https://github.com/alfusinigoj/rabbitmq_messaging_library/actions/workflows/prod-release-pipeline.yml) | [![NuGet](https://img.shields.io/nuget/v/PivotalServices.MoqExtensions.DataReader.svg?style=flat-square)](http://www.nuget.org/packages/PivotalServices.MoqExtensions.DataReader)

### Salient features
- Simple RabbitMQ library which acts as a simple extension to RabbitMQ.Client library

### Package
- Extensions package - [PivotalServices.RabbitMQ.Messaging](https://www.nuget.org/packages/PivotalServices.RabbitMQ.Messaging)

### Usage Instructions
- Install package [PivotalServices.RabbitMQ.Messaging](https://www.nuget.org/packages/PivotalServices.RabbitMQ.Messaging)

#### For adding a publisher/producer of messages

- Add a producer for a message type `MyMessage` as below

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    builder.Services.AddRabbitMQ(cfg => {
        cfg.AddProducer<MyMessage>("exchange-name", "queue-name");
    });
```
- Use a producer as below. Below code is from a controller endpoint where the message is published

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    public ProducerController(IProducer<MyMessage> producer)
    {
        this.producer = producer;
    }
    [HttpGet("send/{text}")]
    public void Send(string text)
    {
        producer.Send(new OutboundMessage<MyMessage>() { Content = new MyMessage { SomeText = text } });
    }
```

#### For adding a consumer of messages

- Add a consumer for a message type `MyMessage` as below

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    builder.Services.AddRabbitMQ(cfg => {
        cfg.AddConsumer<MyMessage>("exchange-name", "queue-name");
    });
```

- To use a consumer, option 1 is to use a singleton hosted service as below

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MessageProcessor>());
```

- And the hosted service is as below which process the consumed message

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    public class MessageProcessor : IHostedService
    {
        private readonly IConsumer<MyMessage> consumer;

        public MessageProcessor(IConsumer<MyMessage> consumer)
        {
            this.consumer = consumer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            consumer.MessageReceived += Received;
            return Task.CompletedTask;
        }

        private void Received(InboundMessage<MyMessage> message)
        {
            try
            {
                //Do Processing of message content here
                consumer.Acknowledge(message);// you can also configure auto acknowledge if needed
            }
            catch (Exception exception)
            {
                consumer.Reject(message);
                logger.LogError(exception, $"Failed processing message, so rejecting", message);
            }
        }
    }
```

- For more details, please refer to the sample project


### Contributions are welcome!

docker run -d --hostname rabbitmq --name rabbitmq -e RABBITMQ_DEFAULT_VHOST=rabbitmq_vhost rabbitmq:3-management



