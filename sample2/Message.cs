
namespace RabbitMQ.Sample2;

public class Message
{
    public string? SomeText { get; set; }
}

public class Queue1Message : Message
{
}

public class Queue2Message : Message
{
}