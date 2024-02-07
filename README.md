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

### Getting Started
- Minimal understanding of use of a message broker. Refer [RabbitMQ Tutorial](https://www.rabbitmq.com/getstarted.html) for better understanding.

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

- If not provided, the default values will be used. Please refer [here](https://github.com/PivotalServicesOss/rabbitmq_messaging_library/blob/master/src/Configuration/ServiceConfiguration.cs) for the defaults.

#### For adding a publisher/producer of messages

- Add a producer for a message type `MyMessage` as below

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    builder.Services.AddRabbitMQ(cfg => {
        cfg.AddProducer<MyMessage>(bindingExchangeName: "exchangeName",
                                       queueName: "queueOneName");
    });
```

- Optionally, you can add optional parameter to control the creation of a DLX, and also configure the queue and exchange properties as below.

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    builder.Services.AddRabbitMQ(cfg => {
        cfg.AddProducer<MyMessage>(bindingExchangeName: "exchangeName",
                                       queueName: "queueOneName",
                                       addDeadLetterQueue: false,
                                       options => {
                                            options.MessageExpirationInSeconds = 0;
                                            options.MaximumQueueLength = 1000;
                                            options.AdditionalArguments.Add("x-queue-type", "quorum");
                                       });
    });
```

- Please refer [here](https://github.com/PivotalServicesOss/rabbitmq_messaging_library/blob/master/src/Configuration/QueueConfiguration.cs) for the defaults and all possible configurations available.

- Below code is from a controller endpoint where the message is published using the producer we configured just above.

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    public ProducerController(IProducer<MyMessage> producer)
    {
        this.producer = producer;
    }
    [HttpPost("send")]
    public void Send(string text)
    {
        var myMessage = new MyMessage { SomeText = text };
        producer.Send(new OutboundMessage<MyMessage>(myMessage));
    }
```

#### For adding a consumer of messages

- Add a consumer for a message type `MyMessage` as below

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    builder.Services.AddRabbitMQ(cfg => {
        cfg.AddConsumer<MyMessage>(bindingExchangeName: "exchangeName",
                                       queueName: "queueOneName");
    });
```

- Optionally, you can add optional parameter to control the creation of a DLX, and also configure the queue and exchange properties as below.

```c#
    using PivotalServices.RabbitMQ.Messaging;
    ...
    builder.Services.AddRabbitMQ(cfg => {
        cfg.AddConsumer<MyMessage>(bindingExchangeName: "exchangeName",
                                       queueName: "queueOneName",
                                       addDeadLetterQueue: false,
                                       options => {
                                            options.MessageExpirationInSeconds = 0;
                                            options.MaximumQueueLength = 1000;
                                            options.AdditionalArguments.Add("x-queue-type", "quorum");
                                       });
    });
```

- Please refer [here](https://github.com/PivotalServicesOss/rabbitmq_messaging_library/blob/master/src/Configuration/QueueConfiguration.cs) for the defaults and all possible configurations available.

> Important Note: Make sure the queue definitions and configurations are exactly same between a consumer and a producer for a same queue, else you may get precondition failures while initializing the connection to the message broker

- Now that we have configured a consumer `IConsumer<MyMessage>`, the consumer will start listening to the queues for incomming messages, when the application starts.

- And finally, we need to subscribe to `MessageReceived` event of the consumer to consume and process the message, as below

```c#
    consumer.MessageReceived += (InboundMessage<MyMessage> message) =>
    {
        try
        {
            using (logger.BeginScope($"{message.CorrelationId}"))
            {
                // Process the message here

                consumer.Acknowledge(message);
            }
        }
        catch
        {
            consumer.Reject(message);
        }
    };
```

- In the above code, we can see that if the message is processed successfully, we acknowledge, else we reject. The rejected message ends up in DLX if configured.

- For more details, please refer to the sample project

### Run RabbitMQ

- Use the below command, but make sure docker is already installed. You can use the official version as well including management options

```bash
    docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.11-management
```

- Access the management UI at http://localhost:15672 with default user and password `guest`

### Contributions are welcome!





