using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FunctionApp_AsyncProcess_Step2
{
    public class Step2
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public Step2(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [FunctionName("Step2")]
        public async Task Run(
            [ServiceBusTrigger("queue2", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message,
            [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages, 
            ILogger log)
        {
            log.LogInformation("C# ServiceBus queue trigger function is processing message with correlationId {CorrelationId}...", message.CorrelationId);

            var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://asyncprocesssink.azurewebsites.net/api/Sink?code=keB1LV5gIE7Z2aAHEEtfi9mtO0vKZTji13uA2/fctEChR7GqRqu1Ig=="));
            request.Content = new StringContent(message.CorrelationId);
            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "newMessage",
                    Arguments = new[] { $"Step 2 processed for requestId {message.CorrelationId}" }
                });

            log.LogInformation("C# ServiceBus queue trigger function has processed message with correlationId {CorrelationId}", message.CorrelationId);
        }
    }
}
