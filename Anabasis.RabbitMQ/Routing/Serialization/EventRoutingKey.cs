using Anabasis.RabbitMQ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RabbitMQPlayground.Routing
{
    public static class EventRoutingKey
    {
        public const string All = "*";
        public const char Separator = '.';

        public static ConcurrentDictionary<Type, string> _eventSerializerCache;

        static EventRoutingKey()
        {
            _eventSerializerCache = new ConcurrentDictionary<Type, string>();
        }

        public static string GetRoutingKeyFromEvent(IRabbitMqEvent @event)
        {

            return _eventSerializerCache.GetOrAdd(@event.GetType(), (type) =>
             {
                 var tokens = GetTokens(@event.GetType());

                 return tokens.Select(token => @event.GetType().GetProperty(token.PropertyInfo.Name).GetValue(@event, null))
                              .Select(obj => null == obj ? All : obj.ToString())
                              .Aggregate((token1, token2) => $"{token1}{Separator}{token2}");

             });

        }

        private static IEnumerable<PropertyToken> GetTokens(Type messageType)
        {

            var properties = messageType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Select(prop => new { attributes = prop.GetCustomAttributes(typeof(RoutingPositionAttribute), true), property = prop })
                                    .Where(selection => selection.attributes.Count() > 0)
                                    .Select(selection => new PropertyToken(((RoutingPositionAttribute)selection.attributes[0]).Position, messageType, selection.property));

            return properties.OrderBy(x => x.Position).ToArray();
        }
    }
}
