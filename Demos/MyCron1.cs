using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace workertest.Demos
{
    public class MyCronJob1 : CronService<MyCronJob1>
    {
        public MyCronJob1(IScheduleConfig<MyCronJob1> config, ILogger<MyCronJob1> logger)
            : base(config, logger)  { }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("CronJob 1 starts.");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("CronJob 1 is stopping.");
            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteScheduleAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation($"{DateTime.Now:hh:mm:ss} CronJob 1 is working.");
            return Task.CompletedTask;
        }
    }
}
