using System;
using System.Collections.Generic;
using System.Linq;

namespace ProxyR.Core.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            => dictionary != null && dictionary.TryGetValue(key, out var value) ? value : default;

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
            => dictionary == null ? default : dictionary.TryGetValue(key, out var value) ? value : defaultValue;

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }
            value = new TValue();
            dictionary.Add(key, value);
            return value;
        }

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> createFunc)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }
            value = createFunc();
            dictionary.Add(key, value);
            return value;
        }

        public static T ToObject<T>(this IDictionary<string, object> dictionary, T target) where T : new()
        {
            var value = new T();
            dictionary.CopyTo(target);
            return value;
        }

        public static void CopyTo<T>(this IDictionary<string, object> dictionary, T target)
        {
            var properties = typeof(T).GetProperties();

            var matchedProperties = properties.Where(p => dictionary.ContainsKey(p.Name));

            foreach (var property in matchedProperties)
            {
                var value = dictionary[property.Name];
                property.SetValue(target, value);
            }
        }
    }

}
