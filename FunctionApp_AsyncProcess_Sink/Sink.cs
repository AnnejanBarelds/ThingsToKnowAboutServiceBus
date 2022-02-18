using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace FunctionApp_AsyncProcess_Sink
{
    public class Sink
    {
        [FunctionName("Sink")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [SignalR(HubName = "LogStream")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function is processing a request...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "newMessage",
                    Arguments = new[] { $"API call in step 2 processed for requestId {requestBody}" }
                });

            log.LogInformation("C# HTTP trigger function processed a request.");

            return new OkObjectResult($"Received {requestBody}");
        }
    }
}
