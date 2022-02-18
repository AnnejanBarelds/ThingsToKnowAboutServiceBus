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
            options.SessionIds.Add("MyWebApp");
            _processor = _client.CreateSessionProcessor("replyqueue", options);
            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;
            await _processor.StartProcessingAsync(cancellationToken);
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            throw new NotImplementedException();
        }

        private async Task ProcessMessageAsync(ProcessSessionMessageEventArgs arg)
        {
            await _logToSignalR.SendMessage($"Received callback on SessionId = {arg.SessionId}");
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