namespace PivotalServices.RabbitMQ.Messaging;

public static class Global
{
    public static List<Type> ConsumerTypes { get; set; } = new List<Type>();
    public static List<Type> ProducerTypes { get; set; } = new List<Type>();
}