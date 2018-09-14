using System;
using System.Diagnostics;
using Messaging.Message;
using Messaging.Queue;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Messaging.Processor
{
    public abstract class PublishingMessageProcessorBase : IMessageProcessor
    {
        protected readonly IInboundMessageQueue inboundMessageQueue;
        private readonly IOutboundMessageQueue outboundMessageQueue;
        private readonly ILogger<PublishingMessageProcessorBase> logger;
        Stopwatch stopWatch = new Stopwatch();

        public PublishingMessageProcessorBase(IInboundMessageQueue inboundMessageQueue, 
                                                IOutboundMessageQueue outboundMessageQueue, 
                                                ILogger<PublishingMessageProcessorBase> logger)
        {
            this.inboundMessageQueue = inboundMessageQueue;
            this.outboundMessageQueue = outboundMessageQueue;
            this.logger = logger;
        }
        protected abstract IOutboundMessageWrapper DoProcessing(IQueueMessage message);

        public Action OnMessageProcessingCompleted;

        public void Start()
        {
            inboundMessageQueue.MessageReceived += (inboundMessageWrapper) =>
            {
                stopWatch.Restart();
                try
                {
                    using (logger.BeginScope($"{inboundMessageWrapper.CorrelationId}"))
                    {
                        var message = JsonConvert.DeserializeObject<IQueueMessage>(inboundMessageWrapper.Message);
                        using (logger.BeginScope($"{message.Key}"))
                        {
                            logger.LogDebug("Begin processing of message");
                            
                            var outboundMessageWrapper = DoProcessing(message);
                            
                            outboundMessageQueue.Publish(outboundMessageWrapper.Message,
                                                            inboundMessageWrapper.CorrelationId,
                                                            inboundMessageWrapper.ReplyTo,
                                                            outboundMessageWrapper.RouteKeys);

                            inboundMessageQueue.Acknowledge(inboundMessageWrapper);

                            logger.LogDebug($"Finished processing of message");
                        }
                    }
                }
                catch (Exception exception)
                {
                    inboundMessageQueue.Reject(inboundMessageWrapper);
                    logger.LogError(exception, $"Failed processing message, so rejecting", inboundMessageWrapper);
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