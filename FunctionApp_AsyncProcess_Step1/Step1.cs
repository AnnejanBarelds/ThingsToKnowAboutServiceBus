using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace FunctionApp_AsyncProcess_Step1
{
    public class Step1
    {
        [FunctionName("Step1")]
        public async Task Run(
            [ServiceBusTrigger("queue1", Connection = "ServiceBusConnectionString")]ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, ServiceBusClient client,
            [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation("C# ServiceBus queue trigger function is processing message with correlationId {CorrelationId}...", message.CorrelationId);

            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await messageActions.CompleteMessageAsync(message);
                var sender = client.CreateSender("queue2");
                await sender.SendMessageAsync(new ServiceBusMessage() { CorrelationId = message.CorrelationId });
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = "newMessage",
                        Arguments = new[] { $"Step 1 processed for requestId {message.CorrelationId}" }
                    });
                ts.Complete();
            }

            log.LogInformation("C# ServiceBus queue trigger function has processed message with correlationId {CorrelationId}", message.CorrelationId);
        }
    }
}
