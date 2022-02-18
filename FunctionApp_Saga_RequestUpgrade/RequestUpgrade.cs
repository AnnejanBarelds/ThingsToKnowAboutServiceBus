using System;
using System.Threading.Tasks;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace FunctionApp_Saga_RequestUpgrade
{
    public class RequestUpgrade
    {
        [FunctionName("RequestUpgrade")]
        public async Task Run([ServiceBusTrigger("upgraderequestsqueue", Connection = "ServiceBusConnectionString")]ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, ServiceBusClient client,
            [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation("C# ServiceBus queue trigger function is processing message with correlationId {CorrelationId}...", message.CorrelationId);

            bool retry = message.DeliveryCount == 1;

            try
            {
                using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    await messageActions.CompleteMessageAsync(message);

                    // Demo of what happens if a transaction is not completed
                    if (message.Body.ToString().Equals("CrossEntityTransactionDemo"))
                    {
                        string action = retry ? "retrying" : "giving up";
                        await signalRMessages.AddAsync(
                            new SignalRMessage
                            {
                                Target = "newMessage",
                                Arguments = new[] { $"Upgrade request throws halfway through a transaction for requestId {message.CorrelationId}; {action}..." }
                            });
                        throw new Exception("BOOM!");
                    }

                    var sender = client.CreateSender("decrementslotssqueue");
                    await sender.SendMessageAsync(new ServiceBusMessage(message.Body) { CorrelationId = message.CorrelationId });
                    await signalRMessages.AddAsync(
                        new SignalRMessage
                        {
                            Target = "newMessage",
                            Arguments = new[] { $"Upgrade request processed for requestId {message.CorrelationId}" }
                        });
                    ts.Complete();
                }
            }
            catch
            {
                if (retry)
                {
                    await messageActions.AbandonMessageAsync(message);
                }
                else
                {
                    await messageActions.DeadLetterMessageAsync(message);
                }
            }

            log.LogInformation("C# ServiceBus queue trigger function has processed message with correlationId {CorrelationId}", message.CorrelationId);
        }
    }
}
