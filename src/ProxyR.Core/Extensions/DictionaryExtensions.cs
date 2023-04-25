using System;
using System.Collections.Generic;
using System.Linq;

namespace ProxyR.Core.Extensions
{
    /// <summary>
    /// Extension methods for the Dictionary class.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Gets the value associated with the specified key from the IDictionary&lt;TKey, TValue&gt; or the default value of the TValue type if the key does not exist.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <param name="dictionary">The IDictionary&lt;TKey, TValue&gt; instance.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="defaultValue">The default value to return if the key does not exist.</param>
        /// <returns>The value associated with the specified key from the IDictionary&lt;TKey, TValue&gt; or the default value of the TValue type if the key does not exist.</returns>
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
               => dictionary != null && dictionary.TryGetValue(key, out var value) ? value : defaultValue;

        /// <summary>
        /// Gets the value associated with the specified key from the dictionary, or creates a new value if the key does not exist.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key, or a new value if the key does not exist.</returns>
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }
            value = new TValue();
            dictionary.TryAdd(key, value);
            return value;
        }

        /// <summary>
        /// Gets the value associated with the specified key from the dictionary, or creates a new value using the specified function if the key does not exist.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="createFunc">The function used to create a new value if the key does not exist.</param>
        /// <returns>The value associated with the specified key.</returns>
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> createFunc)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }
            value = createFunc();
            dictionary.TryAdd(key, value);
            return value;
        }

        /// <summary>
        /// Copies the contents of a dictionary to a target object.
        /// </summary>
        /// <typeparam name="T">The type of the target object.</typeparam>
        /// <param name="dictionary">The dictionary to copy from.</param>
        /// <param name="target">The target object to copy to.</param>
        /// <returns>The target object with the contents of the dictionary copied to it.</returns>
        public static T ToObject<T>(this IDictionary<string, object> dictionary, T target) where T : new()
        {
            var value = new T();
            dictionary.CopyTo(target);
            return value;
        }

        /// <summary>
        /// Copies the values from the dictionary to the target object.
        /// </summary>
        /// <typeparam name="T">The type of the target object.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="target">The target object.</param>
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
