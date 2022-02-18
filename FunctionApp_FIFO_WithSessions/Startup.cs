using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThingsToKnowAboutServiceBus.FunctionApp_FIFO_WithSessions;

[assembly: FunctionsStartup(typeof(Startup))]

namespace ThingsToKnowAboutServiceBus.FunctionApp_FIFO_WithSessions
{

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            //throw new NotImplementedException();
        }
    }
}
