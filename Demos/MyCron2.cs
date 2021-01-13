using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace workertest.Demos
{
    public class MyCronJob2 : CronService<MyCronJob2>
    {
        private readonly IServiceProvider _serviceProvider;

        public MyCronJob2(IScheduleConfig<MyCronJob2> config, ILogger<MyCronJob2> logger, IServiceProvider serviceProvider)
            : base(config, logger)
        {
            _serviceProvider = serviceProvider;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("CronJob 2 starts.");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("CronJob 2 is stopping.");
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteScheduleAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation($"{DateTime.Now:hh:mm:ss} CronJob 2 is working.");
            using var scope = _serviceProvider.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IMyScopedService>();
            await svc.DoWork(stoppingToken);
        }
    }
}
