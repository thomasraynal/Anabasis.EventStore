using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Shared
{
  public static class DictionnaryExtensions
  {
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey,TValue> valueCreator)
    {
      if (!dictionary.TryGetValue(key, out TValue value))
      {
        value = valueCreator(key);
        dictionary.Add(key, value);
      }
      return value;
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
    {
      return dictionary.GetOrAdd(key, (key) => new TValue());
    }
  }
}
