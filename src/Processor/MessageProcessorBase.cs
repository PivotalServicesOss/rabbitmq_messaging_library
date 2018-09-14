using System;
using System.Diagnostics;
using Messaging.Message;
using Messaging.Queue;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Messaging.Processor
{
    public abstract class MessageProcessorBase : IMessageProcessor
    {
        protected readonly IInboundMessageQueue inboundMessageQueue;
        private readonly ILogger<MessageProcessorBase> logger;
        Stopwatch stopWatch = new Stopwatch();
        
        public MessageProcessorBase(IInboundMessageQueue inboundMessageQueue, 
                                        ILogger<MessageProcessorBase> logger)
        {
            this.inboundMessageQueue = inboundMessageQueue;
            this.logger = logger;
        }
        protected abstract void DoProcessing(IQueueMessage message);

        public Action OnMessageProcessingCompleted;

        public void Start()
        {
            inboundMessageQueue.MessageReceived += (messageWrapper) =>
            {
                stopWatch.Restart();
                try
                {
                    using (logger.BeginScope($"{messageWrapper.CorrelationId}"))
                    {
                        var message = JsonConvert.DeserializeObject<IQueueMessage>(messageWrapper.Message);
                        using (logger.BeginScope($"{message.Key}"))
                        {
                            logger.LogDebug("Begin processing of message");
                            
                            DoProcessing(message);
                            inboundMessageQueue.Acknowledge(messageWrapper);

                            logger.LogDebug($"Finished processing of message");
                        }
                    }
                }
                catch (Exception exception)
                {
                    inboundMessageQueue.Reject(messageWrapper);
                    logger.LogError(exception, $"Failed processing message, so rejecting", messageWrapper);
                }
                finally
                {
                    stopWatch.Stop();
                    logger.LogInformation($"Processing time: {stopWatch.ElapsedMilliseconds} ms");
                }
                OnMessageProcessingCompleted?.Invoke();
            };

            inboundMessageQueue.StartConsumption();
        }

        public void Stop()
        {
            inboundMessageQueue.StopConsumption();
        }
    }
}