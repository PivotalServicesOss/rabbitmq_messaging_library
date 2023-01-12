### A simple messaging library that uses RabbitMQ

> Testing not completed yet, so use this with caution. 

#### Kindly raise any issues at [Project Issues](https://github.com/PivotalServicesOss/rabbitmq_messaging_library/issues)

Build | PivotalServices.RabbitMQ.Messaging |
--- | --- |
[![Nuget Pipeline](https://github.com/PivotalServicesOss/rabbitmq_messaging_library/actions/workflows/nuget-pipeline.yml/badge.svg?branch=master)](https://github.com/PivotalServicesOss/rabbitmq_messaging_library/actions/workflows/nuget-pipeline.yml) | [![NuGet](https://img.shields.io/nuget/v/PivotalServices.RabbitMQ.Messaging.svg?style=flat-square)](http://www.nuget.org/packages/PivotalServices.RabbitMQ.Messaging)

### Salient features
- Simple RabbitMQ library which acts as a simple extension to RabbitMQ.Client library

### Package
- Extensions package - [PivotalServices.RabbitMQ.Messaging](https://www.nuget.org/packages/PivotalServices.RabbitMQ.Messaging)

### Usage Instructions
- Install package [PivotalServices.RabbitMQ.Messaging](https://www.nuget.org/packages/PivotalServices.RabbitMQ.Messaging)

#### Connecting to RabbitMQ
- If you don't have a running instance of RabbitMQ, refer to `Run RabbitMQ` section below.
- Add a confuguration section in the `appsettings.json` as below or as appropriate environment variables.

```json
    {
      "RabbitMq": {
        "HostName": "",
        "Vhost": "",
        "Username": "",
        "Password": ""
      }
    }
```

- If not provided, the default values `HostName='localhost', Vhost='/', Username='guest' and Password='guest'` will be used.

#### For adding a publisher/producer of messages

- Add a producer for a message type `MyMessage` as below

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    builder.Services.AddRabbitMQ(cfg => {
        cfg.AddProducer<MyMessage>(exchangeName: "exchangeName",
                                       queueName: "queueOneName",
                                       addDeadLetterQueue: false);
        
        cfg.AddProducer<MyMessage2>(exchangeName: "exchangeName",
                                        queueName: "queueTwoName");
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
        var myMessage = new MyMessage { SomeText = text };
        producer.Send(new OutboundMessage<MyMessage>(myMessage));
    }

    [HttpGet("send2/{text}")]
    public void Send2(string text)
    {
        var myMessage2 = new MyMessage2 { SomeText = text };
        producer2.Send(new OutboundMessage<MyMessage2>(myMessage2));
    }
```

#### For adding a consumer of messages

- Add a consumer for a message type `MyMessage` as below

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    builder.Services.AddRabbitMQ(cfg => {
        cfg.AddConsumer<MyMessage>(exchangeName: "exchangeName",
                                       queueName: "queueOneName",
                                       addDeadLetterQueue: false);

        cfg.AddConsumer<MyMessage2>(exchangeName: "exchangeName",
                                        queueName: "queueTwoName");
    });
```

> Important: Make sure the queue definitions and configurations are same between a consumer and a producer for a same queue, else you may get precondition failures while initializing


- To use a consumer, one simple option is to use a singleton hosted service as below

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
        private readonly IConsumer<MyMessage2> consumer2;

        public MessageProcessor(IConsumer<MyMessage> consumer,
                                IConsumer<MyMessage2> consumer2)
        {
            this.consumer = consumer;
            this.consumer2 = consumer2;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            consumer.MessageReceived += Received;
            consumer2.MessageReceived += Received2;
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

        private void Received2(InboundMessage<MyMessage2> message)
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

### Run RabbitMQ

- Use the below command, but make sure docker is already installed. You can use the official version as well including management options

```bash
    docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.11-management
```

- Access the management UI at http://localhost:15672 with default user and password `guest`

### Contributions are welcome!





