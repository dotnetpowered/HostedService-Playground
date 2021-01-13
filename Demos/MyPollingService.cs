using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace workertest.Demos
{
    public class MyPollingService : PollingService<MyPollingService>
    {
        ILogger<MyPollingService> _logger;

        public MyPollingService(IPollingConfig<MyPollingService> pollingConfig,
            ILogger<MyPollingService> logger) : base(pollingConfig) => _logger = logger;


        protected override Task<bool> ExecutePollerAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
