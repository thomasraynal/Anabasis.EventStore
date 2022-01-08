using Anabasis.Common;
using Anabasis.RabbitMQ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Anabasis.RabbitMQ
{
    public static class EventRoutingKey
    {
        public const string All = "*";
        public const char Separator = '.';

        public static ConcurrentDictionary<Type, PropertyToken[]> _eventPropertyTokenCache;

        static EventRoutingKey()
        {
            _eventPropertyTokenCache = new ConcurrentDictionary<Type, PropertyToken[]>();
        }

        public static string GetRoutingKeyFromEvent(IRabbitMqMessage @event)
        {

            var eventType = @event.GetType();

            var tokens = GetTokens(eventType);

            if(tokens.Length ==0) return $"{eventType.GetReadableNameFromType()}";

            var segments = tokens.Select(token => @event.GetType().GetProperty(token.PropertyInfo.Name).GetValue(@event, null))
                                 .Select(obj => null == obj ? All : obj.ToString())
                                 .Aggregate((token1, token2) => $"{token1}{Separator}{token2}");

            return $"{eventType.GetReadableNameFromType()}.{segments}";

        }

        private static PropertyToken[] GetTokens(Type messageType)
        {
            return _eventPropertyTokenCache.GetOrAdd(messageType, (type) =>
            {
                var properties = messageType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .Select(prop => new { attributes = prop.GetCustomAttributes(typeof(RoutingPositionAttribute), true), property = prop })
                                        .Where(selection => selection.attributes.Count() > 0)
                                        .Select(selection => new PropertyToken(((RoutingPositionAttribute)selection.attributes[0]).Position, messageType, selection.property));

                return properties.OrderBy(propertyToken => propertyToken.Position).ToArray();

            });
        }
    }
}
