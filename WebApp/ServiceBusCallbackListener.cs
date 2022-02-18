using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;

namespace WebApp
{
    public class ServiceBusCallbackListener : IHostedService
    {
        private readonly ServiceBusClient _client;
        private readonly LogToSignalR _logToSignalR;
        private readonly ILogger _logger;
        private ServiceBusSessionProcessor? _processor;

        public ServiceBusCallbackListener(IAzureClientFactory<ServiceBusClient> serviceBusClientFactory, LogToSignalR logToSignalR, ILogger<ServiceBusCallbackListener> logger)
        {
            _client = serviceBusClientFactory.CreateClient("Client");
            _logToSignalR = logToSignalR;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = new ServiceBusSessionProcessorOptions
            {
                AutoCompleteMessages = true
            };
            _processor = _client.CreateSessionProcessor("replyqueue", options);
            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;
            await _processor.StartProcessingAsync(cancellationToken);
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(ProcessSessionMessageEventArgs arg)
        {
            var state = await arg.GetSessionStateAsync();
            var sessionState = state != null ? state.ToObjectFromJson<SessionState>() : new SessionState();
            var result = arg.Message.Body.ToObjectFromJson<JobResult>();
            if (sessionState.SetJobResult(result))
            {
                await _logToSignalR.SendMessage($"All jobs returned a value on reply session = {arg.SessionId}");
                await _logToSignalR.SendMessage($"Job 1 success = {sessionState.Job1Result!.Success} on reply session = {arg.SessionId}");
                await _logToSignalR.SendMessage($"Job 2 success = {sessionState.Job2Result!.Success} on reply session = {arg.SessionId}");
            }
            await arg.SetSessionStateAsync(BinaryData.FromObjectAsJson(sessionState));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_processor != null && !_processor.IsClosed)
            {
                await _processor.StopProcessingAsync();
                await _processor.CloseAsync();
            }
        }
    }
}