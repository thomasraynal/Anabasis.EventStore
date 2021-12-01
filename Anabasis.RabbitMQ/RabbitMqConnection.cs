using Anabasis.Api;
using Microsoft.Extensions.Logging;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Anabasis.EventStore.Shared;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqConnection: IDisposable
    {
        private readonly IModel _model;
        private readonly IAutorecoveringConnection _connection;
        private readonly ILogger<RabbitMqConnection> _logger;

        private readonly object _syncRoot = new object();

        private readonly ConcurrentQueue<BasicReturnEventArgs> _returnQueue;
        private readonly RabbitMqConnectionOptions _rabbitMqConnectionOptions;
        private readonly RetryPolicy _retryPolicy;
        private readonly AnabasisAppContext _appContext;

        private readonly bool _mustReconnect = false;

        private string _blockedConnectionReason = null;

        public bool IsBlocked => _blockedConnectionReason != null;

        public RabbitMqConnection(RabbitMqConnectionOptions rabbitMqConnectionOptions,  
            AnabasisAppContext appContext,
            ILoggerFactory loggerFactory,
            RetryPolicy retryPolicy)
        {
            _rabbitMqConnectionOptions = rabbitMqConnectionOptions;
            _retryPolicy = retryPolicy;
            _appContext = appContext;

            _logger = loggerFactory.CreateLogger<RabbitMqConnection>();
            _returnQueue = new ConcurrentQueue<BasicReturnEventArgs>();

            var connectionFactory = new ConnectionFactory()
            {
                HostName = _rabbitMqConnectionOptions.HostName,
                UserName = _rabbitMqConnectionOptions.UserName,
                Password = _rabbitMqConnectionOptions.Password,
                Port = _rabbitMqConnectionOptions.Port,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            };

            _connection = (IAutorecoveringConnection)connectionFactory.CreateConnection(appContext.ApplicationName);

            _connection.ConnectionBlocked += ConnectionBlocked;
            _connection.ConnectionUnblocked += ConnectionUnblocked;
            _connection.CallbackException += CallbackException;

            _connection.RecoverySucceeded += RecoverySucceeded;
            _connection.ConnectionShutdown += ConnectionShutdown;
           

            _model = _connection.CreateModel();
            _model.BasicReturn += (sender, args) => _returnQueue.Enqueue(args);
            _model.BasicQos(prefetchSize: 0, prefetchCount: _rabbitMqConnectionOptions.PrefetchCount, global: true);
            _model.ConfirmSelect();

        }

        private void ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogInformation($"AMQP connection shutdown - {nameof(ShutdownEventArgs)} => {e?.ToJson()}");
        }

        private void RecoverySucceeded(object sender, EventArgs e)
        {
            _logger.LogInformation($"AMQP connection recovery success - {nameof(EventArgs)} => {e?.ToJson()}");
        }

        private void CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "An exception occured in an AMQP connection process");
        }

        public IBasicProperties GetBasicProperties()
        {
            return DoWithChannel(channel =>
            {
                var props = channel.CreateBasicProperties();
                props.Persistent = true;
                return props;
            });
        }

        private void ConnectionUnblocked(object sender, EventArgs e)
        {
            _logger.LogInformation($"AMQP connection unblocked - {nameof(EventArgs)} => {e?.ToJson()}");
            _blockedConnectionReason = null;
        }

        private void ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            _logger.LogInformation($"AMQP connection blocked - {nameof(ConnectionBlockedEventArgs)} => {e?.ToJson()}");
            _blockedConnectionReason = e.Reason;
        }
        public void DoWithChannel(Action<IModel> action)
        {
            DoWithChannel(channel => { action(channel); return 0; });
        }

        public T DoWithChannel<T>(Func<IModel, T> function)
        {
            if (IsBlocked)
                throw new InvalidOperationException($"Connection is blocked : {_blockedConnectionReason}");

            lock (_syncRoot)
            {
                var returnedValue = _retryPolicy.Execute(() =>
                {
                    while (_returnQueue.TryDequeue(out var ev));

                    var result = function(_model);

                    if (_returnQueue.Count != 0)
                    {
                        var basicReturnEventArgs = new List<BasicReturnEventArgs>();

                        while (_returnQueue.TryDequeue(out var arg))
                        {
                            basicReturnEventArgs.Add(arg);
                        }
                           
                        if (basicReturnEventArgs.Count != 0)
                        {
                            if (basicReturnEventArgs.Count == 1)
                                throw new InvalidOperationException(basicReturnEventArgs[0].ReplyText);
                            else
                                throw new AggregateException(basicReturnEventArgs.Select(arg => new InvalidOperationException(arg.ReplyText)));
                        }
                    }

                    return result;
                });

                return returnedValue;
            }
        }

        public void Dispose()
        {
            if (_model != null)
            {
                _model.Dispose();
            }

            try
            {
                _connection.Abort(TimeSpan.FromMilliseconds(10));
            }
            finally
            {
                _connection.Close(TimeSpan.FromMilliseconds(10));
            }
        }
    }
}
