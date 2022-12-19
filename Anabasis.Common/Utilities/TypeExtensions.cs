using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.Common
{
    public static class TypeExtensions
    {
        private static readonly ConcurrentDictionary<Type, string> _typeToFriendlyNameCache = new ConcurrentDictionary<Type, string>();
        private static readonly ConcurrentDictionary<string,Type> _friendlyNameToTypeCache = new ConcurrentDictionary<string,Type>();

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

        public static string GetUniqueIdFromType(this object @object, string? prefix = null, string? postfix = null)
        {
            return $"{prefix ?? string.Empty}{@object.GetType().Name}_{Guid.NewGuid()}{postfix ?? string.Empty}";
        }

        public static string GetReadableNameFromType(this object @object)
        {
            return @object.GetType().GetReadableNameFromType();
        }

        public static string GetReadableNameFromType(this Type type)
        {

            if (_typeToFriendlyNameCache.TryGetValue(type, out string result))
                return result;

            if (type.IsGenericType)
            {
                var friendlyName = type.Name;
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                var genericNames = type.GetGenericArguments().Select(t => t.GetReadableNameFromType());
                result = $"{friendlyName}<{string.Join(", ", genericNames)}>";
            }
            else
            {
                result = _typeToFriendlyName.TryGetValue(type, out string friendlyName)
                    ? friendlyName
                    : type.Name;
            }

            _typeToFriendlyNameCache[type] = result;
            return result;
        }

        public static Type GetTypeFromReadableName(this string typeReadableName)
        {
            return _friendlyNameToTypeCache.GetOrAdd(typeReadableName, (readableName) =>
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetTypes().FirstOrDefault(candidateType => candidateType.GetReadableNameFromType() == readableName);
                    if (type != null)
                        return type;
                }

                throw new InvalidOperationException($"Could not resolve readable name => {readableName}");
            });


        }

    }
}
