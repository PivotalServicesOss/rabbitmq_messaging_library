namespace Messaging.Message
{
    public class QueueMessage : IQueueMessage
    {
        public string Key { get; set; }
        public string Body { get; set; }
    }
}