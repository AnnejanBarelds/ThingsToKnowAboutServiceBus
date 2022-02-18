using System;
using System.Threading.Tasks;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace FunctionApp_RequestReply_Replier
{
    public class Function1
    {
        [FunctionName("Function1")]
        public async Task Run(
            [ServiceBusTrigger("requestqueue", Connection = "ServiceBusConnectionString")]ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, ServiceBusClient client,
            [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation("C# ServiceBus queue trigger function is processing message...");
            await Task.Delay(1000);
            await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "newMessage",
                    Arguments = new[] { $"Sending reply to request on reply session {message.ReplyToSessionId}..." }
                });
            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await messageActions.CompleteMessageAsync(message);
                var sender = client.CreateSender("replyqueue");
                var sessionId = !string.IsNullOrWhiteSpace(message.ReplyToSessionId) ? message.ReplyToSessionId : Guid.NewGuid().ToString();
                await sender.SendMessageAsync(new ServiceBusMessage($"Reply created at {DateTimeOffset.Now}") { SessionId = sessionId });
                ts.Complete();
            }
        }
    }
}
