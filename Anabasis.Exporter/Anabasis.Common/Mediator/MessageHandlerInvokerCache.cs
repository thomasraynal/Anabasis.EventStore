using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Anabasis.Common.Mediator
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
        return base.Equals(obj);
      }
      public override int GetHashCode()
      {
        return HandlerType.GetHashCode() ^ MessageHandlerType.GetHashCode();
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

      return _methodInfoCache.GetOrAdd(key, handlerType.GetMethod("Handle", new[] { messageHandlerType }));
    }
  }
}

