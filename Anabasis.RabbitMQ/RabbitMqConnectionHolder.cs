using Anabasis.Api;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqConnectionHolder : IDisposable
    {
        private readonly IModel _channel;
        private readonly object _syncRoot = new object();
        private readonly ConcurrentQueue<BasicReturnEventArgs> _returnQueue = new ConcurrentQueue<BasicReturnEventArgs>();
        private readonly IAutorecoveringConnection _connection;

        internal bool _mustReconnect = false;
        readonly RetryPolicy _retryPolicy;
        private readonly ITopicPubSub _pubsub;
        private readonly ILogger _logger;
        internal readonly string _hostName;
        internal readonly string _user;
        internal readonly AnabasisAppContext _appContext;
        private string _blockedConnectionReason = null;
        public bool IsBlocked => _blockedConnectionReason != null;

        public RabbitMqConnectionHolder(
            RabbitMqConnectionOptions rabbitMqConnectionOptions, 
            AnabasisAppContext appContext, ushort prefetchCount = 2, RetryPolicy retryPolicy = null)
            : this(settings.Host, settings.User, settings.Password, appContext, settings.Port, prefetchCount, retryPolicy)
        { }

        internal RabbitMqConnectionHolder(string hostName, string user, string password, AnabasisAppContext appContext, int port = 5672, ushort prefetchCount = 2, RetryPolicy retryPolicy = null)
        {
            _retryPolicy = retryPolicy ?? new RabbitMqRetryPolicy(10, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
            _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));

            _hostName = hostName;
            _user = user;
            _pubsub = appContext?.PubSub;
            _logger = appContext?.Logger;

            var factory = new ConnectionFactory()
            {
                HostName = hostName,
                UserName = user,
                Password = password,
                Port = port,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                //DispatchConsumersAsync = true // only for async consuming version http://gigi.nullneuron.net/gigilabs/asynchronous-rabbitmq-consumers-in-net/
            };

            _connection = (IAutorecoveringConnection)factory.CreateConnection(appContext.);

            //https://www.rabbitmq.com/connection-blocked.html
            _connection.ConnectionBlocked += ConnectionBlocked;
            _connection.ConnectionUnblocked += ConnectionUnblocked;
            _connection.CallbackException += _connection_CallbackException;
            _connection.RecoverySucceeded += (object sender, EventArgs e) => PublishCreatedMessageAsync(_connection);
            _connection.ConnectionShutdown += (object sender, ShutdownEventArgs e) => PublishClosedMessageAsync(_connection, e.ReplyText);

            _channel = _connection.CreateModel();
            _channel.BasicReturn += (sender, args) => _returnQueue.Enqueue(args);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: prefetchCount, global: true);
            _channel.ConfirmSelect();

            //var _connectionCap = _connection.ClientProperties["capabilities"] as Dictionary<string, bool>;
            //var isBlocked = false;
            //_connectionCap?.TryGetValue("connection.blocked", out isBlocked);
            //if (isBlocked)
            //    _blockedConnectionReason = "Connection created blocked already";

            // THIS LINE WAS PURE EVIL : it produced a second connection to rabbitmq 
            // which was preventing the app from closing on CTRL-C
            //_connection.Init(); 
        }

        private void _connection_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            _appContext.Logger.LogException(e.Exception);
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
            _blockedConnectionReason = null;
        }
        private void ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            _blockedConnectionReason = e.Reason;
        }


       // private RabbitMqConnectionCreated _lastCreatedMessage;
        private void PublishCreatedMessageAsync(IAutorecoveringConnection connection)
        {
            //var message = new RabbitMqConnectionCreated(connection.ClientProvidedName);
            //_lastCreatedMessage = message;
            //_pubsub?.PublishAsync(message); // not awaited
            //_logger?.LogMessage(message);

        }

        private void PublishClosedMessageAsync(IAutorecoveringConnection connection, string reason)
        {
            //var message = new RabbitMqConnectionClosed(connection.ClientProvidedName, reason, _lastCreatedMessage);
            //_pubsub?.PublishAsync(message); // not awaited
            //_logger?.LogMessage(message);
        }


        public void DoWithChannel(Action<IModel> action)
        {
            DoWithChannel(channel => { action(channel); return 0; });
        }

        public T DoWithChannel<T>(Func<IModel, T> function)
        {
            if (IsBlocked)
                throw new BeezUPRabbitMqException($"Connection is blocked : {_blockedConnectionReason}");

            lock (_syncRoot)
            {
                var res = _retryPolicy.ExecuteAction(() =>
                {
                    while (_returnQueue.TryDequeue(out var ev))
                        ;

                    var result = function(_channel);

                    if (_returnQueue.Count != 0)
                    {
                        var list = new List<BasicReturnEventArgs>();
                        while (_returnQueue.TryDequeue(out var arg))
                            list.Add(arg);

                        if (list.Count != 0)
                        {
                            if (list.Count == 1)
                                throw new BeezUPRabbitMqException(list[0].ReplyText);
                            else
                                throw new AggregateException(list.Select(a => new BeezUPRabbitMqException(a.ReplyText)));
                        }
                    }

                    return result;
                });

                return res;
            }
        }

        public void Dispose()
        {
            if (_channel != null)
            {
                _channel.Dispose();
            }

            try
            {
                _connection.Abort(10);
            }
            finally
            {
                _connection.Close(10);
            }
        }
    }
}
