//using Anabasis.Common;
//using Lamar;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Anabasis.EventStore.Standalone
//{
//    public abstract class BaseActorBuilder<TActor, TRegistry>
//    {
//        protected readonly Dictionary<Type, Action<Container, IActor>> _busToRegisterTo;

//        private BaseActorBuilder()
//        {
//            _busToRegisterTo = new Dictionary<Type, Action<Container, IActor>>();
//        }

//        public StatelessActorBuilder<TActor, TRegistry> WithBus<TBus>(Action<TActor, TBus> onStartup) where TBus : IBus
//        {
//            var busType = typeof(TBus);

//            if (_busToRegisterTo.ContainsKey(busType))
//                throw new InvalidOperationException($"ActorBuilder already has a reference to a bus of type {busType}");

//            var onRegistration = new Action<Container, IActor>((container, actor) =>
//            {
//                var bus = container.GetInstance<TBus>();

//                onStartup((TActor)actor, bus);

//            });

//            _busToRegisterTo.Add(busType, onRegistration);

//            return this;
//        }
//    }
//}
