namespace PivotalServices.RabbitMQ.Messaging;

public class ServiceConfiguration
{
    public const string ConfigRoot = "RabbitMq";
    public string HostName { get; set; } = "localhost";
    public string Vhost { get; set; } = "/";
    public string Uri { get; set; } = "http://localhost:5672/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}