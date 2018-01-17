namespace Messaging.Message
{
    public interface IQueueMessage
    {
        string Key { get; set; }
        string Body { get; set; }
    }
}