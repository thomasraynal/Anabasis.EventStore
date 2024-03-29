﻿using Microsoft.Extensions.Logging;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Polly;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.IO;
using Anabasis.Common;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqConnection : IRabbitMqConnection
    {
        private IModel? _model;
       
        private readonly ILogger<RabbitMqConnection>? _logger;

        private readonly object _syncRoot = new();

        private readonly ConcurrentQueue<BasicReturnEventArgs> _returnQueue;
        private readonly RabbitMqConnectionOptions _rabbitMqConnectionOptions;
        private readonly RetryPolicy _retryPolicy;
        private readonly AnabasisAppContext _appContext;
        private readonly List<ulong> _deliveredMessages;

        private string? _blockedConnectionReason = null;

        public bool IsBlocked => _blockedConnectionReason != null;
        public bool IsOpen => AutoRecoveringConnection != null && AutoRecoveringConnection.IsOpen;

        public IAutorecoveringConnection? AutoRecoveringConnection { get; private set; }

        public RabbitMqConnection(RabbitMqConnectionOptions rabbitMqConnectionOptions,
            AnabasisAppContext appContext,
            ILoggerFactory? loggerFactory = null,
            RetryPolicy? retryPolicy = null)
        {
            _rabbitMqConnectionOptions = rabbitMqConnectionOptions;

            if (null == retryPolicy)
            {
                retryPolicy = Policy.Handle<OperationInterruptedException>()
                                    .Or<SocketException>()
                                    .Or<NotSupportedException>()
                                    .Or<IOException>()
                                    .Or<TimeoutException>()
                                    .Or<AlreadyClosedException>()
                                    .Or<BrokerUnreachableException>()
                                    .WaitAndRetry(20, (_) => TimeSpan.FromSeconds(5));
            }

            _retryPolicy = retryPolicy;
            _appContext = appContext;
            _logger = loggerFactory?.CreateLogger<RabbitMqConnection>();
            _returnQueue = new ConcurrentQueue<BasicReturnEventArgs>();
            _deliveredMessages = new List<ulong>();

        }

        public void Connect()
        {
            _retryPolicy.Execute(() =>
            {
                try
                {
                    var connectionFactory = new ConnectionFactory()
                    {
                        HostName = _rabbitMqConnectionOptions.HostName,
                        UserName = _rabbitMqConnectionOptions.Username,
                        Password = _rabbitMqConnectionOptions.Password,
                        Port = _rabbitMqConnectionOptions.Port,
                        AutomaticRecoveryEnabled = true,
                        DispatchConsumersAsync = true,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                    };

                    AutoRecoveringConnection = (IAutorecoveringConnection)connectionFactory.CreateConnection(_appContext.ApplicationName);

                    AutoRecoveringConnection.ConnectionBlocked += ConnectionBlocked;
                    AutoRecoveringConnection.ConnectionUnblocked += ConnectionUnblocked;

                    _model = AutoRecoveringConnection.CreateModel();
                    _model.BasicReturn += (sender, args) =>
                    {
                        _returnQueue.Enqueue(args);
                    };
                    _model.BasicQos(prefetchSize: _rabbitMqConnectionOptions.PrefetchSize, prefetchCount: _rabbitMqConnectionOptions.PrefetchCount, global: true);
                    _model.ConfirmSelect();

                }

                catch (Exception exception)
                {
                    _logger?.LogError(exception, "Error trying to create RabbitMQ connection");

                    throw;
                }

            });
        }

        public IBasicProperties GetBasicProperties()
        {
            return DoWithChannel(channel =>
            {
                var properties = channel.CreateBasicProperties();
                return properties;
            });
        }

        private void ConnectionUnblocked(object sender, EventArgs e)
        {
            _blockedConnectionReason = null;
        }

        private void ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            _blockedConnectionReason = e.Reason;
        }

        public void DoWithChannel(Action<IModel> action)
        {
            DoWithChannel(channel => { action(channel); return 0; });
        }

        public void Acknowledge(ulong deliveryTag)
        {
            if (null == _model)
                throw new ArgumentNullException("Model");

            if (_deliveredMessages.Contains(deliveryTag)) return;

            _model.BasicAck(deliveryTag, multiple: false);

            _deliveredMessages.Add(deliveryTag);
        }

        public void NotAcknowledge(ulong deliveryTag)
        {
            if (null == _model)
                throw new ArgumentNullException("Model");

            if (_deliveredMessages.Contains(deliveryTag)) return;

            _model.BasicNack(deliveryTag, multiple: false, requeue: true);

            _deliveredMessages.Add(deliveryTag);
        }

        public T DoWithChannel<T>(Func<IModel, T> function)
        {

            if (null == _model)
                throw new ArgumentNullException("Model");

            if (IsBlocked)
                throw new InvalidOperationException($"Connection is blocked : {_blockedConnectionReason}");

            lock (_syncRoot)
            {
                var returnedValue = _retryPolicy.Execute(() =>
                {
                    while (_returnQueue.TryDequeue(out var ev)) ;

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
            if (AutoRecoveringConnection != null)
            {
                AutoRecoveringConnection.ConnectionBlocked += ConnectionBlocked;
                AutoRecoveringConnection.ConnectionUnblocked += ConnectionUnblocked;
            }

            if (_model != null)
            {
                _model.Dispose();
            }

            if (AutoRecoveringConnection != null)
            {
                try
                {
                    AutoRecoveringConnection.Abort(TimeSpan.FromMilliseconds(10));
                }
                finally
                {
                    AutoRecoveringConnection.Close(TimeSpan.FromMilliseconds(10));
                }
            }
        }
    }
}
