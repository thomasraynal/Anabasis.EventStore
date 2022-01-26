//using Anabasis.EventStore.EventProvider;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Anabasis.EventStore
//{
//    public class EventTypeProviderFactory : IEventTypeProviderFactory
//    {
//        private readonly Dictionary<Type, IEventTypeProvider> _eventTypeProviders;

//        public EventTypeProviderFactory()
//        {
//            _eventTypeProviders = new Dictionary<Type, IEventTypeProvider>();
//        }

//        public IEventTypeProvider Get(Type actorType)
//        {
//            return _eventTypeProviders[actorType];
//        }

//        public void Add<TActor>(IEventTypeProvider eventTypeProvider)
//        {
//            _eventTypeProviders.Add(typeof(TActor), eventTypeProvider);
//        }
//    }
//}
