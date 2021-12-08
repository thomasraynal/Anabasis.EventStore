using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IActor
    {
        string Id { get; }
        bool IsConnected { get; }
        Task WaitUntilConnected(TimeSpan? timeout = null);
        public Task Emit<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) 
            where TEvent : IEvent;
    }
}
