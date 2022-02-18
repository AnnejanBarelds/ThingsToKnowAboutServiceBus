using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;

namespace FunctionApp_RequestReply_Requestor
{
    public class Requestor
    {
        private readonly ServiceBusClient _serviceBusClient;

        public Requestor(IAzureClientFactory<ServiceBusClient> serviceBusClientFactory)
        {
            _serviceBusClient = serviceBusClientFactory.CreateClient("Client");
        }

        [FunctionName("Request")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function is processing a request...");

            var replySession = Guid.NewGuid().ToString();

            var sender = _serviceBusClient.CreateSender("requestqueue");
            await sender.SendMessageAsync(new ServiceBusMessage($"Request message created at {DateTimeOffset.Now}") { ReplyToSessionId = replySession});

            return await GetReply(req, replySession);
        }

        [FunctionName("GetReply")]
        public async Task<IActionResult> GetReply(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "GetReply/{replySession}")] HttpRequest req,
            string replySession,
            ILogger log)
        {
            return await GetReply(req, replySession);
        }

        private async Task<IActionResult> GetReply(HttpRequest req, string replySession)
        {
            var replyListener = await _serviceBusClient.AcceptSessionAsync("replyqueue", replySession);
            var replyMessage = await replyListener.ReceiveMessageAsync(TimeSpan.FromSeconds(3));
            if (replyMessage != null)
            {
                string reply = replyMessage.Body.ToString();
                return new OkObjectResult(reply);
            }
            else
            {
                var uri = GetCallbackUri(req, $"/api/GetReply/{replySession}");
                return new AcceptedResult(new Uri($"/api/GetReply?replySession={replySession}", UriKind.Relative), uri.ToString());
            }
        }

        private static Uri GetCallbackUri(HttpRequest request, string path)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = request.Host.Host,
                Port = request.Host.Port.GetValueOrDefault(-1),
                Path = path
            };
            return uriBuilder.Uri;
        }
    }
}
