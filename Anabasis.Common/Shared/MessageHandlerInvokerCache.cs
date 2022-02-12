using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Anabasis.Common
{
    public class MessageHandlerInvokerCache
    {

        class MessageHandlerInvokerCacheKey
        {
            public MessageHandlerInvokerCacheKey(Type handlerType, Type messageHandlerType)
            {
                HandlerType = handlerType;
                MessageHandlerType = messageHandlerType;
            }

            public Type HandlerType { get; }
            public Type MessageHandlerType { get; }

            public override bool Equals(object obj)
            {
                return obj is MessageHandlerInvokerCacheKey && base.Equals(obj);
            }
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = HandlerType.GetHashCode();
                    hashCode = (hashCode * 397) ^ MessageHandlerType.GetHashCode();
                    return hashCode;
                }
            }
        }

        private readonly ConcurrentDictionary<MessageHandlerInvokerCacheKey, MethodInfo> _methodInfoCache;

        public MessageHandlerInvokerCache()
        {
            _methodInfoCache = new ConcurrentDictionary<MessageHandlerInvokerCacheKey, MethodInfo>();
        }

        public MethodInfo GetMethodInfo(Type handlerType, Type messageHandlerType)
        {
            var key = new MessageHandlerInvokerCacheKey(handlerType, messageHandlerType);

            return _methodInfoCache.GetOrAdd(key, (cacheKey) =>
            {

                var methodInfo = handlerType.GetMethod("Handle", new[] { cacheKey.MessageHandlerType });

                if (null == methodInfo)
                {
                    methodInfo = messageHandlerType.GetMethods().Where(method => null != method.GetCustomAttribute<EventSink>()).FirstOrDefault();
                }

                return methodInfo;

            });

        }
    }
}

