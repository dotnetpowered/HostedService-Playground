using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace workertest.Demos
{
    public class MyPollingService : PollingService<MyPollingService>
    {
        public MyPollingService(IPollingConfig<MyPollingService> pollingConfig,
            ILogger<MyPollingService> logger) : base(pollingConfig, logger) { }


        protected override async Task<bool> ExecutePollerAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation($"{DateTime.Now:hh:mm:ss} MyPollingService is working.");
            return true;
        }
    }
}
