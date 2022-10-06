//using Anabasis.Common.Utilities;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Anabasis.Common
//{

//    public class ServiceCollectionEventTypeProvider : IEventTypeProvider
//    {
//        private readonly Dictionary<string, Type?> _eventTypeCache;
//        private readonly IServiceProvider _serviceProvider;

//        public ServiceCollectionEventTypeProvider(IServiceProvider serviceProvider)
//        {
//            _eventTypeCache = new Dictionary<string, Type?>();
//            _serviceProvider = serviceProvider;
//        }

//        public bool CanHandle(IEvent @event)
//        {
//            return _eventTypeCache.Values.Any(value => value == @event.GetType());
//        }

//        //refacto
//        public Type[] GetAll()
//        {
//            return _serviceProvider.GetServices<IHaveEntityId>().Select(type => type.GetType()).ToArray();
//        }

//        public Type? GetEventTypeByName(string name)
//        {
//            return _eventTypeCache.GetOrAdd(name, (key) =>
//            {
//                var type = _serviceProvider.GetServices<IHaveEntityId>()
//                                 .FirstOrDefault(type => type.GetType().FullName == name);

//                if (null == type) return null;

//                return type.GetType();

//            });
//        }
//    }

//    public class ServiceCollectionEventTypeProvider<TAggregate> : IEventTypeProvider where TAggregate : class, IAggregate
//    {
//        private readonly Dictionary<string, Type?> _eventTypeCache;
//        private readonly IServiceProvider _serviceProvider;

//        public ServiceCollectionEventTypeProvider(IServiceProvider serviceProvider)
//        {
//            _eventTypeCache = new Dictionary<string, Type?>();
//            _serviceProvider = serviceProvider;
//        }

//        //refacto
//        public Type[] GetAll()
//        {
//            return _serviceProvider.GetServices<IAggregateEvent<TAggregate>>().Select(type => type.GetType()).ToArray();
//        }

//        public Type? GetEventTypeByName(string name)
//        {
//            return _eventTypeCache.GetOrAdd(name, (key) =>
//            {
//                var type = _serviceProvider.GetServices<IAggregateEvent<TAggregate>>()
//                                 .FirstOrDefault(type => type.GetType().FullName == name);

//                if (null == type) return null;

//                return type.GetType();

//            });
//        }
//    }
//}
