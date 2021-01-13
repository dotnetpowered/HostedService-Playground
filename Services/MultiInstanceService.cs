using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace workertest
{
    public class MultiInstanceService<T> : BackgroundService where T : IHostedService
    {
        protected ILogger<T> Logger { get; init; }
        private readonly IServiceProvider _provider;

        public MultiInstanceService(IMultiInstanceConfig<T> config, ILogger<T> logger, IServiceProvider provider)
        {
            Logger = logger;
            this._provider = provider;
            this.Instances = config.Instances;
        }

        protected virtual int Instances { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            List<Task> tasks = new();
            IHostedService service = _provider.GetService(typeof(T)) as IHostedService;

            for (int i=0;i<Instances;i++)
            {
                var task = Task.Run(() => {
                    service.StartAsync(stoppingToken).Wait(stoppingToken);
                    service.StopAsync(stoppingToken).Wait();
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks.ToArray());
        }

    }

    public interface IMultiInstanceConfig<T>
    {
        int Instances { get; set; }
    }

    public class MultiInstanceConfig<T> : IMultiInstanceConfig<T>
    {
        public int Instances { get; set; }
    }

    public static class MultiInstanceExtensions
    {
        public static IServiceCollection AddMultiInstanceService<T>(
            this IServiceCollection services, Action<IMultiInstanceConfig<T>> options) where T : IHostedService
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), @"Please provide Multi-Instance Configurations.");
            }
            var config = new MultiInstanceConfig<T>();
            options.Invoke(config);

            services.AddSingleton<IMultiInstanceConfig<T>>(config);
            services.AddHostedService<MultiInstanceService<T>>();
            return services;
        }
    }
}
