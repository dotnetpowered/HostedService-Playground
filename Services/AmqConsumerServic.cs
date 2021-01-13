using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace workertest
{
    public interface IAmqConfiguration<T> : IPollingConfig<T>
    {
        string BrokerUri { get; set; }
        string QueueName { get; set; }
        string ExceptionQueueName { get; set; }
    }

    public class AmqConfiguration<T> : IAmqConfiguration<T>, IMultiInstanceConfig<T>
    {
        public string BrokerUri { get; set; }
        public string QueueName { get; set; }
        public string ExceptionQueueName { get; set; }
        public int Instances { get; set; }
        public int PollingDelay { get; set; }
    }

    public abstract class AmqConsumerService<T> : PollingService<T>
    {
        // ActiveMQ Consumer Resources
        private IConnection _connection;
        private ISession _session;
        private IMessageConsumer _consumer;

        private IAmqConfiguration<T> _amqConfig;
        TimeSpan timeOutTimeSpan = new TimeSpan(0, 0, 1);
        private readonly ILogger<T> _logger;

        protected AmqConsumerService(ILogger<T> logger,
                                     IAmqConfiguration<T> amqConfig) : base(amqConfig)
        {
            this._logger = logger;
            this._amqConfig = amqConfig;
        }

        protected override async Task<bool> ExecutePollerAsync(CancellationToken stoppingToken)
        {
            ITextMessage mqTextMessage;
            // Receive message
            try
            {
                // Create AMQ connection, consumer, etc if needed
                IMessageConsumer consumer = GetConsumer();

                mqTextMessage = (ITextMessage)consumer.Receive(timeOutTimeSpan);
                if (mqTextMessage == null)
                    return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while recieving message from queue {_amqConfig.BrokerUri} {_amqConfig.QueueName}", ex);
                TeardownAMQ();
                return true;
            }

            // Process Message
            try
            {
                _logger.LogInformation($"Processing message {mqTextMessage.NMSMessageId} received from {_amqConfig.BrokerUri} {_amqConfig.QueueName}");
                await ProcessMessageAsync(mqTextMessage, stoppingToken);
                mqTextMessage.Acknowledge();
                return false; // Don't perform polling delay, so we can immediately check for another message;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during processing of message {mqTextMessage.NMSMessageId}", ex);

                if (_amqConfig.ExceptionQueueName != null && SendToExceptionQueue(mqTextMessage))
                {
                    mqTextMessage.Acknowledge();
                    return false; // Don't perform polling delay, so we can immediately check for another message;
                }
                else
                {
                    // Unhandled exception and/or no exception queue to send message
                    // Disconnect from AMQ - which leaves message on queue and allows retry
                    // Could lead to infinite loop - so you should use the exception queue option
                    TeardownAMQ();
                    return true;
                }

            }
        }

        protected abstract Task<bool> ProcessMessageAsync(ITextMessage queueMessage, CancellationToken stoppingToken);

        private IMessageConsumer GetConsumer()
        {
            if (_consumer == null)
            {
                var factory = new ConnectionFactory(_amqConfig.BrokerUri);
                _connection = factory.CreateConnection();
                //_connection.ExceptionListener += new ExceptionListener(OnConnectionException);
                _connection.Start();
                _session = _connection.CreateSession(AcknowledgementMode.IndividualAcknowledge);
                IDestination destinationQueue = _session.GetQueue(_amqConfig.QueueName);
                _consumer = _session.CreateConsumer(destinationQueue);
            }
            return _consumer;
        }

        private bool SendToExceptionQueue(ITextMessage message)
        {
            try
            {
                _logger.LogError($"Message {message.NMSMessageId} being moved to the Exception Queue {_amqConfig.ExceptionQueueName}.");

                var destination = SessionUtil.GetDestination(_session, _amqConfig.ExceptionQueueName);
                // Create a message producer
                using (var producer = _session.CreateProducer(destination))
                {
                    producer.DeliveryMode = Apache.NMS.MsgDeliveryMode.Persistent;
                    // Send a message to the destination
                    producer.Send(message);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while sending message {message.NMSMessageId} to exception queue {_amqConfig.BrokerUri} {_amqConfig.ExceptionQueueName}", ex);
                return false;
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            TeardownAMQ();
            return base.StopAsync(cancellationToken);
        }

        private void TeardownAMQ()
        {
            try
            {
                if (_session != null)
                {
                    _session.Close();
                    _session.Dispose();
                    _session = null;
                }

                if (_consumer != null)
                {
                    _consumer.Dispose();
                    _consumer = null;
                }

                if (_connection != null)
                {
                    _connection.Stop();
                    _connection.Close(); 
                    _connection.Dispose(); 
                    _connection = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during AMQ cleanup of {_amqConfig.BrokerUri} {_amqConfig.QueueName}", ex);
                _session = null;
                _connection = null;
                _consumer = null;
            }
        }
    }

    public static class AmqConsumerExtensions
    {
        public static IServiceCollection AddAmqConsumer<T>(
            this IServiceCollection services, Action<AmqConfiguration<T>> options) where T : AmqConsumerService<T>
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), @"Please provide Polling Configurations.");
            }
            var config = new AmqConfiguration<T>();
            options.Invoke(config);

            services.AddSingleton<IAmqConfiguration<T>>(config);
            services.AddHostedService<MultiInstanceService<T>>();
            return services;
        }
    }
}
