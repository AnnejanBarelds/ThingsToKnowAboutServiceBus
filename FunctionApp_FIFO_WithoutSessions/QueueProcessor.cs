using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ThingsToKnowAboutServiceBus.FunctionApp_FIFO_WithoutSessions
{
    public class QueueProcessor
    {
        private Random _rnd = new Random();

        [FunctionName("QueueProcessor")]
        public async Task Run([ServiceBusTrigger("fifoqueue", Connection = "ServiceBusConnectionString")]ServiceBusReceivedMessage message, [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages, ILogger log)
        {
            await Task.Delay(_rnd.Next(750, 1250));
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {message.MessageId}");

            await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "newMessage",
                    Arguments = new[] { $"Processed message: {message.MessageId}; Enqueue time: {message.EnqueuedTime.ToString("hh:mm:ss.fff tt")}" }
                });
        }
    }
}