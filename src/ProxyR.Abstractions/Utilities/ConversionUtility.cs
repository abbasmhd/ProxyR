using ProxyR.Abstractions.Extensions;
using ProxyR.Core.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace ProxyR.Abstractions.Utilities
{
    public class ConversionUtility
    {
        /// <summary>
        /// Converts the primitive value to the desired type, accounting for nullable types.
        /// </summary>
        public static object Convert(object value, Type targetType)
        {
            switch (value)
            {
                // Simply pass through nulls.
                case null:
                    return null;

                // Use a type-converter for most conversions from a String.
                // This works well for types such as a Guid.
                case string stringValue when targetType != typeof(object) && targetType != typeof(DateTime):
                    {
                        var converter = TypeDescriptor.GetConverter(targetType);
                        var convertedValue = converter.ConvertFromInvariantString(stringValue);
                        return convertedValue;
                    }
            }

            // Get the Underlying Type version of the type, and convert into that.
            var underlyingType = targetType.GetNullableUnderlyingType();
            var changedValue = System.Convert.ChangeType(value, underlyingType);
            return changedValue;
        }

        /// <summary>
        /// Copies an object's properties into a dictionary.
        /// </summary>
        public static IDictionary<string, object> ObjectToDictionary(object source, bool useDbNulls = false)
        {
            var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var properties = source.GetType().GetProperties();
            foreach (var property in properties)
            {
                dictionary[property.Name] = property.GetValue(source) ?? (useDbNulls ? DBNull.Value : null);
            }
            return dictionary;
        }

        /// <summary>
        /// Creates a DataRow, for a DataTable from the properties of an entity object.
        /// </summary>
        public static DataRow EntityToDataRow<TEntity>(TEntity entity, DataTable table)
        {
            var row = table.NewRow();
            row.SetValues(entity);
            return row;
        }

        /// <summary>
        /// Creates a DataTable from a Dictionary. Will have a Key column and a Value column.
        /// </summary>
        public DataTable DictionaryToDataTable(IDictionary<string, object> dictionary)
        {
            var dataTable = new DataTable();

            dataTable.Columns.Add("Key", typeof(string));
            dataTable.Columns.Add("Value");

            foreach (var (key, value) in dictionary)
            {
                var row = dataTable.NewRow();
                row[0] = key;
                row[1] = value;
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        /// <summary>
        /// Creates an object, and sets the properties to the values 
        /// found in the dictionary where the key/value pairs have 
        /// keys matching the property names.
        /// </summary>
        public T DictionaryToObject<T>(IDictionary<string, object> dictionary) where T : new()
        {
            var properties = typeof(T).GetProperties()
                .ToDictionary(p => p.Name, StringComparer.InvariantCultureIgnoreCase);

            var result = new T();

            foreach (var (key, value) in dictionary)
            {
                if (!properties.TryGetValue(key, out var property))
                {
                    continue;
                }

                var convertedValue = Convert(value, property.PropertyType);
                property.SetValue(result, convertedValue);
            }

            return result;
        }

        /// <summary>
        /// Converts a string of key-value pairs, in a format similar to a connection-string, 
        /// and will return a dictionary. The the connection-string is NULL or empty, will 
        /// return an empty dictionary.
        /// </summary>
        public IDictionary<string, string> ConnectionStringToDictionary(string connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString))
            {
                return new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }

            // Deconstruct the connection-string into key-value pairs.
            var pairs = connectionString
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split(new[] { '=' }, 2));

            // Reconstruct into a dictionary.
            var result = pairs.ToDictionary(
                keySelector: p => p[0].Trim(),
                elementSelector: p => p[1].Trim(),
                comparer: StringComparer.InvariantCultureIgnoreCase);

            return result;
        }
    }
}
