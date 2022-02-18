using FunctionApp_RequestReply_Requestor;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(Startup))]

namespace FunctionApp_RequestReply_Requestor
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddAzureClients(builder =>
            {
                builder.AddServiceBusClient("Endpoint=sb://annejan.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=do5trUGsZ0m1Q1Pk3SGoOslHIFZYIvPht23Qdupv3gQ=")
                .WithName("Client");
            });
        }
    }
}
