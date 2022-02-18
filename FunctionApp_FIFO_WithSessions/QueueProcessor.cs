using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace ThingsToKnowAboutServiceBus.FunctionApp_FIFO_WithSessions
{
    public class QueueProcessor
    {
        private Random _rnd = new Random();

        [FunctionName("QueueProcessor")]
        public async Task Run([ServiceBusTrigger("fifosessionqueue", Connection = "ServiceBusConnectionString", IsSessionsEnabled = true)]ServiceBusReceivedMessage message, ServiceBusSessionMessageActions sessionActions, ServiceBusClient client,
            [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages, ILogger log)
        {
            await Task.Delay(_rnd.Next(750, 1250));
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {message.MessageId}");

            var sessionState = await sessionActions.GetSessionStateAsync();

            if (message.MessageId.Equals("5") && sessionState == null)
            {
                // We're mimicking some sort of scenario where a message cannot be settled (e.g. abandoned or completed), such as a crash
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = "newMessage",
                        Arguments = new[] { $"Crashed on message: {message.MessageId} in session {message.SessionId}; Enqueue time: {message.EnqueuedTime.ToString("hh:mm:ss.fff tt")}; will retry..." }
                    });
                await sessionActions.SetSessionStateAsync(new BinaryData("5"));
                Environment.Exit(-1);
            }

            await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "newMessage",
                    Arguments = new[] { $"Processed message: {message.MessageId} in session {message.SessionId}; Enqueue time: {message.EnqueuedTime.ToString("hh:mm:ss.fff tt")}" }
                });
            await sessionActions.CompleteMessageAsync(message);
        }
    }
}