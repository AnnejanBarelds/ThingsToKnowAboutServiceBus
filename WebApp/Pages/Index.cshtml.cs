using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;

namespace WebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly LogToSignalR _logToSignalR;
        private readonly TelemetryClient _telemetryClient;

        public IndexModel(
            ILogger<IndexModel> logger,
            IAzureClientFactory<ServiceBusClient> serviceBusClientFactory,
            LogToSignalR logToSignalR,
            TelemetryClient telemetryClient
            )
        {
            _logger = logger;
            _logToSignalR = logToSignalR;
            _serviceBusClient = serviceBusClientFactory.CreateClient("Client");
            _telemetryClient = telemetryClient;
        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Starting process...");
            var sender = _serviceBusClient.CreateSender("queue1");
            var message = new ServiceBusMessage($"Process step 1 message at {DateTimeOffset.UtcNow}");
            await sender.SendMessageAsync(message);
            return RedirectToPage("./Index");
        }

        public async Task<EmptyResult> OnGetTestAsync()
        {
            _logger.LogInformation("Starting process...");
            var sender = _serviceBusClient.CreateSender("fifoqueue");
            var message = new ServiceBusMessage($"Process step 1 message at {DateTimeOffset.UtcNow}");
            await sender.SendMessageAsync(message);
            var logSender = new LogToSignalR();
            //await logSender.InitAsync();
            await logSender.SendMessage("Message sent.");
            return new EmptyResult();
        }

        public async Task<EmptyResult> OnGetFifoAsync()
        {
            _logger.LogInformation("Starting process...");
            var sender = _serviceBusClient.CreateSender("fifoqueue");
            for (int i = 0; i < 10; i++)
            {
                await sender.SendMessageAsync(new ServiceBusMessage() { MessageId = i.ToString()});
            }
            return new EmptyResult();
        }

        public async Task<EmptyResult> OnGetFifoSingletonAsync()
        {
            _logger.LogInformation("Starting process...");
            var sender = _serviceBusClient.CreateSender("fifosingletonqueue");
            for (int i = 0; i < 10; i++)
            {
                await sender.SendMessageAsync(new ServiceBusMessage() { MessageId = i.ToString()});
            }
            return new EmptyResult();
        }

        public async Task<EmptyResult> OnGetFifoSessionAsync()
        {
            _logger.LogInformation("Starting process...");
            var sender = _serviceBusClient.CreateSender("fifosessionqueue");
            var sessionId = Guid.NewGuid().ToString();
            for (int i = 0; i < 10; i++)
            {
                await sender.SendMessageAsync(new ServiceBusMessage() { MessageId = i.ToString(), SessionId = sessionId });
            }
            return new EmptyResult();
        }

        public async Task<EmptyResult> OnGetRequestCallbackAsync()
        {
            _logger.LogInformation("Starting process...");
            await _logToSignalR.SendMessage($"Sending request message...");
            var sender = _serviceBusClient.CreateSender("requestqueue");
            await sender.SendMessageAsync(new ServiceBusMessage($"Request message created at {DateTimeOffset.Now}") { ReplyToSessionId = "MyWebApp" });
            return new EmptyResult();
        }

        public async Task<EmptyResult> OnGetRequestReplyAsync()
        {
            _logger.LogInformation("Starting process...");
            await _logToSignalR.SendMessage($"Sending request message...");
            var sender = _serviceBusClient.CreateSender("requestqueue");
            var replySession = Guid.NewGuid().ToString();
            var replyListener = await _serviceBusClient.AcceptSessionAsync("replyqueue", replySession);
            await sender.SendMessageAsync(new ServiceBusMessage($"Request message created at {DateTimeOffset.Now}") { ReplyToSessionId = replySession });
            var replyMessage = await replyListener.ReceiveMessageAsync(TimeSpan.FromSeconds(3));
            if (replyMessage != null)
            {
                await _logToSignalR.SendMessage($"Received reply on SessionId = {replyMessage.SessionId}");
            }
            else
            {
                await _logToSignalR.SendMessage($"Timed out while waiting for reply");
            }
            return new EmptyResult();
        }

        public async Task<EmptyResult> OnGetCompensatingTransactionsAsync()
        {
            _logger.LogInformation("Starting process...");
            var correlationId = Guid.NewGuid().ToString();
            await _logToSignalR.SendMessage($"Sending request message with Id {correlationId}...");
            var sender = _serviceBusClient.CreateSender("upgraderequestsqueue");
            await sender.SendMessageAsync(new ServiceBusMessage($"Request message created at {DateTimeOffset.Now}") { CorrelationId = correlationId });
            return new EmptyResult();
        }

        public async Task<EmptyResult> OnGetCrossEntityTransactionsAsync()
        {
            _logger.LogInformation("Starting process...");
            var correlationId = Guid.NewGuid().ToString();
            await _logToSignalR.SendMessage($"Sending request message with Id {correlationId}...");
            var sender = _serviceBusClient.CreateSender("upgraderequestsqueue");
            await sender.SendMessageAsync(new ServiceBusMessage("CrossEntityTransactionDemo") { CorrelationId = correlationId });
            return new EmptyResult();
        }

        public async Task<EmptyResult> OnGetEndToEndTracingAsync()
        {
            var operation = _telemetryClient.StartOperation<RequestTelemetry>("DistributedOperation");
            _logger.LogInformation("Starting process...");
            var correlationId = Guid.NewGuid().ToString();
            await _logToSignalR.SendMessage($"Sending request message with Id {correlationId}...");
            var sender = _serviceBusClient.CreateSender("queue1");
            await sender.SendMessageAsync(new ServiceBusMessage() { CorrelationId = correlationId });
            _telemetryClient.StopOperation(operation);
            return new EmptyResult();
        }
    }
}