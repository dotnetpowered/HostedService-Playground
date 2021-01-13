using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Microsoft.Extensions.Logging;

namespace workertest.Demos
{
    public class MyAmqService : AmqConsumerService<MyAmqService>
    {
        public MyAmqService(ILogger<MyAmqService> Logger, IAmqConfiguration<MyAmqService> amqConfig) : base(Logger, amqConfig)
        {
        }

        protected override Task<bool> ProcessMessageAsync(ITextMessage queueMessage, CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
