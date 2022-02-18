using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace FunctionApp_Saga_IncreaseMonthlyFee
{
    public class IncreaseMonthlyFee
    {
        private readonly Random _random = new Random();

        [FunctionName("IncreaseMonthlyFee")]
        public async Task Run([ServiceBusTrigger("increasefeequeue", Connection = "ServiceBusConnectionString")]ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, ServiceBusClient client,
            [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation("C# ServiceBus queue trigger function is processing message with correlationId {CorrelationId}...", message.CorrelationId);

            if (_random.NextDouble() >= 0.5)
            {
                // We randomly throw in 50% of the cases
                await messageActions.DeadLetterMessageAsync(message);
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = "newMessage",
                        Arguments = new[] { $"Monthly fee increase failed for requestId {message.CorrelationId}" }
                    });
            }
            else
            {
                await messageActions.CompleteMessageAsync(message);
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        Target = "newMessage",
                        Arguments = new[] { $"Monthly fee increase processed for requestId {message.CorrelationId}" }
                    });
            }

            log.LogInformation("C# ServiceBus queue trigger function has processed message with correlationId {CorrelationId}", message.CorrelationId);
        }
    }
}
