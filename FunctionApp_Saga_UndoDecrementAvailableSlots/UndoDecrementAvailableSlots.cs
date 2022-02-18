using System;
using System.Threading.Tasks;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace FunctionApp_Saga_UndoDecrementAvailableSlots
{
    public class UndoDecrementAvailableSlots
    {
        [FunctionName("UndoDecrementAvailableSlots")]
        public async Task Run([ServiceBusTrigger("undodecrementslotsqueue", Connection = "ServiceBusConnectionString")]ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, ServiceBusClient client,
            [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation("C# ServiceBus queue trigger function is processing message with correlationId {CorrelationId}...", message.CorrelationId);

            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await messageActions.CompleteMessageAsync(message);
                var sender = client.CreateSender("upgradecancellationqueue");
                await sender.SendMessageAsync(new ServiceBusMessage(message.Body) { CorrelationId = message.CorrelationId });
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = "newMessage",
                        Arguments = new[] { $"Undo slot count decrement request processed for requestId {message.CorrelationId}" }
                    });
                ts.Complete();
            }

            log.LogInformation("C# ServiceBus queue trigger function has processed message with correlationId {CorrelationId}", message.CorrelationId);
        }
    }
}
