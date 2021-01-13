using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using workertest.Demos;

namespace workertest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // https://medium.com/@frankkerrigan/creating-a-service-for-both-linux-systemd-and-windows-service-fe4ddfa68597
                .UseWindowsService()
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddScoped<IMyScopedService, MyScopedService>()
                        .AddHostedService<MyLongRunningService>()
                        .AddPollingService<MyPollingService>(c=>
                        {
                            c.PollingDelay = 1000;
                        })
                        .AddMultiInstanceService<MyAmqService>(c=>
                        {
                            c.Instances = 5;
                        })
                        .AddAmqConsumer<MyAmqService>(c=>
                        {
                            c.PollingDelay = 1000;
                            c.BrokerUri = "tcp://localhost";
                            c.ExceptionQueueName = "MyQueue.Exception";
                            c.QueueName = "MyQueue";
                            c.Instances = 5;
                        })
                        .AddCronJob<MyCronJob1>(c =>
                        {
                            c.CronExpression = @"*/5 * * * *";
                        })
                        // MyCronJob2 calls the scoped service MyScopedService
                        .AddCronJob<MyCronJob2>(c =>
                        {
                            c.CronExpression = @"* * * * *";
                        });
                });
    }
}
