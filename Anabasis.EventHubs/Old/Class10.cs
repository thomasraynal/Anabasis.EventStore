using Anabasis.EventHubs.Old.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Old
{
    public class GenericConsumable<T> : IConsumable<T>
        where T : class
    {
        Action<T> _consumedAction;
        ActionAsync<T> _consumedActionAsync;

        public GenericConsumable(T content)
        {
            Content = content;
        }

        public GenericConsumable(T content, Action<T> consumedAction)
        {
            Content = content;
            _consumedAction = consumedAction;
        }

        public GenericConsumable(T content, ActionAsync<T> consumedActionAsync)
        {
            Content = content;
            _consumedActionAsync = consumedActionAsync;
        }

        public GenericConsumable(T content, Action<T> consumedAction, ActionAsync<T> consumedActionAsync)
        {
            Content = content;
            _consumedAction = consumedAction;
            _consumedActionAsync = consumedActionAsync;
        }

        public T Content { get; private set; }

        public void SetConsumed()
        {
            if (_consumedAction != null) _consumedAction(Content);
            else _consumedActionAsync?.Invoke(Content).Wait();
        }

        public Task SetConsumedAsync()
        {
            if (_consumedActionAsync != null)
                return _consumedActionAsync(Content);
            else
            {
                _consumedAction?.Invoke(Content);
                return Task.CompletedTask;
            }
        }
    }
}
