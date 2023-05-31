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
        /// Converts an object to a specified type.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <param name="targetType">The type to convert the object to.</param>
        /// <returns>The converted object.</returns>
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
        /// Converts an object to a dictionary of string and object pairs.
        /// </summary>
        /// <param name="source">The object to convert.</param>
        /// <param name="useDbNulls">Whether to use DBNull.Value for null values.</param>
        /// <returns>A dictionary of string and object pairs.</returns>
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
        /// Converts an entity to a DataRow.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="entity">The entity to convert.</param>
        /// <param name="table">The DataTable to use.</param>
        /// <returns>A DataRow containing the entity's data.</returns>
        public static DataRow EntityToDataRow<TEntity>(TEntity entity, DataTable table)
        {
            var row = table.NewRow();
            row.SetValues(entity);
            return row;
        }

        /// <summary>
        /// Converts a dictionary to a DataTable.
        /// </summary>
        /// <param name="dictionary">The dictionary to convert.</param>
        /// <returns>A DataTable containing the dictionary's key-value pairs.</returns>
        public static DataTable DictionaryToDataTable(IDictionary<string, object> dictionary)
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
        /// Converts a dictionary to an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object to be created.</typeparam>
        /// <param name="dictionary">The dictionary to be converted.</param>
        /// <returns>An object of type T.</returns>
        public static T DictionaryToObject<T>(IDictionary<string, object> dictionary) where T : new()
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
        /// Converts a connection string into a dictionary of key-value pairs.
        /// </summary>
        /// <param name="connectionString">The connection string to convert.</param>
        /// <returns>A dictionary of key-value pairs.</returns>
        public static IDictionary<string, string> ConnectionStringToDictionary(string connectionString)
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
