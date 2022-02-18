using Microsoft.Azure.SignalR.Management;
using Microsoft.AspNetCore.SignalR;

namespace WebApp
{
    public class LogToSignalR
    {
        private ServiceHubContext? _hubContext = null;

        public async Task SendMessage(string message)
        {
            if (_hubContext == null)
            {
                var serviceManager = new ServiceManagerBuilder().WithOptions(option =>
                {
                    option.ConnectionString = "Endpoint=https://logstream.service.signalr.net;AccessKey=KBtUgJeXnG3lEmBI4i1aexmb7IJIjIs7Wn5STA+rL2c=;Version=1.0;";
                    option.ServiceTransportType = ServiceTransportType.Transient;
                }).BuildServiceManager();

                _hubContext = await serviceManager.CreateHubContextAsync("LogStream", default);
            }
            await _hubContext.Clients.All.SendAsync("newMessage", message);
        }
    }
}
