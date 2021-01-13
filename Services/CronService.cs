using System;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace workertest
{
    public abstract class CronService<T> : BackgroundService
    {
        private readonly IScheduleConfig<T> _config;
        private readonly CronExpression _cronExpression;

        public CronService(IScheduleConfig<T> config)
        {
            _config = config;
            _cronExpression = CronExpression.Parse(config.CronExpression);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Logic is loosely based on https://codeburst.io/schedule-cron-jobs-using-hostedservice-in-asp-net-core-e17c47ba06
            var delay = TimeSpan.MaxValue;
            while (delay.TotalMilliseconds >= 0 && !stoppingToken.IsCancellationRequested)
            {
                var next = _cronExpression.GetNextOccurrence(DateTimeOffset.Now, _config.TimeZone);
                if (next.HasValue)
                {
                    delay = next.Value - DateTimeOffset.Now;
                    // Don't run if next is in the past
                    if (delay.TotalMilliseconds <= 0)   
                    {
                        await Task.Delay(delay, stoppingToken);
                        if (!stoppingToken.IsCancellationRequested)
                            await ExecuteSchedule(stoppingToken);
                    }
                }
            }
        }

        protected abstract Task ExecuteSchedule(CancellationToken stoppingToken);       
    }

    public interface IScheduleConfig<T>
    {
        string CronExpression { get; set; }
        TimeZoneInfo TimeZone { get; set; }
    }

    public class ScheduleConfig<T> : IScheduleConfig<T>
    {
        public ScheduleConfig()
        {
            TimeZone = TimeZoneInfo.Local;
        }

        public string CronExpression { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
    }

    public static class ScheduledServiceExtensions
    {
        public static IServiceCollection AddCronJob<T>(this IServiceCollection services, Action<IScheduleConfig<T>> options) where T : CronService<T>
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), @"Please provide Schedule Configurations.");
            }
            var config = new ScheduleConfig<T>();
            options.Invoke(config);
            if (string.IsNullOrWhiteSpace(config.CronExpression))
            {
                throw new ArgumentNullException(nameof(ScheduleConfig<T>.CronExpression), @"Empty Cron Expression is not allowed.");
            }

            services.AddSingleton<IScheduleConfig<T>>(config);
            services.AddHostedService<T>();
            return services;
        }
    }
}
