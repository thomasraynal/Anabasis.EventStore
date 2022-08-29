using Anabasis.Common.Contracts;
using Anabasis.Common.Queue;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public class ThreadDispatcher
    {
        private readonly IDispacherStrategy _dispacherStrategy;
        private readonly Func<IGrouping<string, IEvent[]>, Task> _onMessageGroups;
        private readonly Thread _thread;
        private readonly CompositeDisposable _cleanUp;
        private readonly IKillSwitch _killSwitch;

        public Exception? LastError { get; private set; }
        public bool IsFaulted { get; private set; }
        public ILogger? Logger { get; }
        public string Owner { get; }
        public string Id { get; }

        public ThreadDispatcher(string ownerId,
            IDispacherStrategy dispacherStrategy,
            IQueueBuffer queueBuffer,
            Func<IGrouping<string, IEvent[]>, Task> onMessageGroups,
            ILoggerFactory? loggerFactory = null,
            IKillSwitch? killSwitch = null)
        {
            _dispacherStrategy = dispacherStrategy;
            _onMessageGroups = onMessageGroups;

            Logger = loggerFactory?.CreateLogger(GetType());
            Owner = ownerId;
            Id = $"{nameof(DispatchQueue)}_{ownerId}_{Guid.NewGuid()}";

            _killSwitch = killSwitch ?? new KillSwitch();

            _thread = new Thread(HandleWork)
            {
                IsBackground = true,
                Name = Id,
            };

            _thread.Start();

            Logger?.LogDebug("{0} started", Id);

        }

        private async void HandleWork()
        {


        }

    }
}
