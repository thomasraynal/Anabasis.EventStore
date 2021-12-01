using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.RabbitMQ
{
    public static class TypeExtensions
    {
        private static readonly ConcurrentDictionary<Type, string> _friendlyNamesCache = new ConcurrentDictionary<Type, string>();

        private static readonly Dictionary<Type, string> _typeToFriendlyName = new Dictionary<Type, string>
        {
            {typeof(string), "string"},
            {typeof(object), "object"},
            {typeof(bool), "bool"},
            {typeof(byte), "byte"},
            {typeof(char), "char"},
            {typeof(decimal), "decimal"},
            {typeof(double), "double"},
            {typeof(short), "short"},
            {typeof(int), "int"},
            {typeof(long), "long"},
            {typeof(sbyte), "sbyte"},
            {typeof(float), "float"},
            {typeof(ushort), "ushort"},
            {typeof(uint), "uint"},
            {typeof(ulong), "ulong"},
            {typeof(void), "void"}
        };

        public static string GetTypeReadableName(this object @object)
        {
            return @object.GetType().GetTypeReadableName();
        }

        public static string GetTypeReadableName(this Type type)
        {

            if (_friendlyNamesCache.TryGetValue(type, out string result))
                return result;

            if (type.IsGenericType)
            {
                var friendlyName = type.Name;
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                var genericNames = type.GetGenericArguments().Select(t => t.GetTypeReadableName());
                result = $"{friendlyName}<{string.Join(", ", genericNames)}>";
            }
            else
            {
                result = _typeToFriendlyName.TryGetValue(type, out string friendlyName)
                    ? friendlyName
                    : type.Name;
            }

            _friendlyNamesCache[type] = result;
            return result;
        }

    }
}
