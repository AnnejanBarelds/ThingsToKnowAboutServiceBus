using System;
using System.Threading.Tasks;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace FunctionApp_FanOutFanIn_Subscriber
{
    public class Subscriber
    {
        private readonly ILogger _logger;
        private readonly Random _rnd = new Random();

        public Subscriber(ILogger<Subscriber> logger)
        {
            _logger = logger;
        }

        [FunctionName("Subscriber1")]
        public async Task RunSubscriber1([ServiceBusTrigger("fanouttopic", "subscription1", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, ServiceBusClient client,
            [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            _logger.LogInformation("C# ServiceBus queue trigger function is processing message with Id {MessageId}...", message.MessageId);

            await Task.Delay(_rnd.Next(250, 1250));

            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = new { JobId = 1, Success = true };
                await signalRMessages.AddAsync(
                     new SignalRMessage
                     {
                         Target = "newMessage",
                         Arguments = new[] { $"Subscriber 1 handled the message on reply session {message.ReplyToSessionId}" }
                     });
                var sender = client.CreateSender(message.ReplyTo);
                await sender.SendMessageAsync(new ServiceBusMessage(BinaryData.FromObjectAsJson(result)) { SessionId = message.ReplyToSessionId });
                await messageActions.CompleteMessageAsync(message);
                ts.Complete();
            }
        }

        [FunctionName("Subscriber2")]
        public async Task RunSubscriber2([ServiceBusTrigger("fanouttopic", "subscription2", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, ServiceBusClient client,
            [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            _logger.LogInformation("C# ServiceBus queue trigger function is processing message with Id {MessageId}...", message.MessageId);

            await Task.Delay(_rnd.Next(250, 1250));

            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = new { JobId = 2, Success = true };
                await signalRMessages.AddAsync(
                     new SignalRMessage
                     {
                         Target = "newMessage",
                         Arguments = new[] { $"Subscriber 2 handled the message on reply session {message.ReplyToSessionId}" }
                     });
                var sender = client.CreateSender(message.ReplyTo);
                await sender.SendMessageAsync(new ServiceBusMessage(BinaryData.FromObjectAsJson(result)) { SessionId = message.ReplyToSessionId });
                await messageActions.CompleteMessageAsync(message);
                ts.Complete();
            }
        }
    }
}
