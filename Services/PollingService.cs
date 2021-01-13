using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace workertest
{
    public abstract class PollingService<T> : BackgroundService
    {
        protected ILogger<T> Logger { get; init; }

        protected PollingService(IPollingConfig<T> pollingConfig, ILogger<T> logger)
        {
            Logger = logger;
            this.PollingDelay = pollingConfig.PollingDelay;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                bool delay = await ExecutePollerAsync(stoppingToken);
                if (delay && !stoppingToken.IsCancellationRequested)
                    await Task.Delay(PollingDelay, stoppingToken);
            }
        }

        protected virtual int PollingDelay { get;set; }

        protected abstract Task<bool> ExecutePollerAsync(CancellationToken stoppingToken);
    }

    public interface IPollingConfig<T>
    {
        int PollingDelay { get; set; }
    }

    public class PollingConfig<T> : IPollingConfig<T>
    {
        public int PollingDelay { get; set; }
    }

    public static class PollingExtensions
    {
        public static IServiceCollection AddPollingService<T>(
            this IServiceCollection services, Action<IPollingConfig<T>> options) where T : PollingService<T>
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), @"Please provide Polling Configurations.");
            }
            var config = new PollingConfig<T>();
            options.Invoke(config);

            services.AddSingleton<IPollingConfig<T>>(config);
            services.AddHostedService<T>();
            return services;
        }
    }

}
