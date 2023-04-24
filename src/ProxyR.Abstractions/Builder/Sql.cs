using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ProxyR.Abstractions.Builder
{
    public static class Sql
    {

        /// <summary>
        ///     Inputs a string, that is to be used as an SQL identifier, object name, or keyword,
        ///     replacing characters with '_' that can be used as an SQL injection attack.
        /// </summary>
        public static string Sanitize(string identifier) => Regex.Replace(identifier.Trim(), "[^A-Za-z0-9_]", "_");
        
        public static string ColumnReferences(params string[] names) => String.Join(", ", names.Select(name => $"[{name}]"));

        public static string ColumnLines(params string[] names) => String.Join("\n, ", names.Select(name => $"[{name}]"));

        public static string[] SplitIdentifierParts(string chainOfParts) => chainOfParts.Split('.').Select(ids => ids.Trim('[', ']')).ToArray();

        public static (string Schema, string Object) GetSchemaAndObjectName(string identifier)
        {
            var parts = SplitIdentifierParts(identifier);

            return parts.Length switch
            {
                1 => ("dbo", parts[0]),
                2 => (parts[0], parts[1]),
                _ => throw new InvalidOperationException($"The given SQL identifier [{identifier}] cannot be split."),
            };
        }

        public static object[] GetPropertyValues(object obj, IEnumerable<PropertyInfo> properties) => properties.Select(p => p.GetValue(obj)).ToArray();

        public static string ParenthesisLines(params string[] contents) => String.Join("\n, ", contents.Select(r => $"({r})"));

        public static string Values(IEnumerable<object> values) => String.Join(", ", values.SelectQuoted());

        public static string Values(params object[] values) => Values((IEnumerable<object>)values);

        public static string CommaLines(params string[] lines) => String.Join("\n, ", lines);

        public static string ColumnDefinition(string columnName, string type, bool? isNullable = null, string defaultExpression = null, int columnNamePadding = 0, bool doPadding = false, string collation = null)
        {
            var columnPart = $"[{columnName}]".PadRight(doPadding ? columnNamePadding : 0);

            var collationPart = String.Empty;

            if (type.IndexOf("char", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                collationPart = $"COLLATE {collation ?? "DATABASE_DEFAULT"}";
            }

            var nullablePart = String.Empty;

            if (isNullable != null)
            {
                nullablePart = isNullable == true ? "NULL" : "NOT NULL";
            }

            var defaultPart = String.Empty;

            if (defaultExpression != null)
            {
                defaultPart = $"= {defaultExpression}";
            }

            var result = String.Join(" ",
                    columnPart,
                    type.PadRight(doPadding ? 16 : 0),
                    collationPart.PadRight(doPadding ? 20 : 0),
                    nullablePart.PadRight(doPadding ? 4 : 0),
                    defaultPart)
                .Trim();
            return result;
        }

        public static IEnumerable<string> SelectQuoted(this IEnumerable<object> values)
        {
            foreach (var value in values)
            {
                yield return Quote(value);
            }
        }

        public static string Quote(object value)
        {
            if (value is JValue jValue)
            {
                value = jValue.Type switch
                {
                    JTokenType.Bytes => (byte[])jValue,
                    JTokenType.Integer => int.Parse(jValue.ToString(CultureInfo.InvariantCulture)),
                    JTokenType.Float => float.Parse(jValue.ToString(CultureInfo.InvariantCulture)),
                    JTokenType.Boolean => bool.Parse(jValue.ToString(CultureInfo.InvariantCulture)),
                    JTokenType.Null or JTokenType.Undefined => null,
                    JTokenType.Date => DateTime.Parse(jValue.ToString(CultureInfo.InvariantCulture)),
                    JTokenType.String or JTokenType.TimeSpan or JTokenType.Guid or JTokenType.Uri => jValue.ToString(CultureInfo.InvariantCulture),
                    _ => throw new InvalidOperationException($"Unknown JTokenType [{value}]."),
                };
            }

            if (value == null || value == DBNull.Value)
            {
                return "NULL";
            }

            return value switch
            {
                string stringValue  => $"'{stringValue.Replace("'", "''")}'",
                DateTime dateTime   => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'",
                bool isTrue         => isTrue ? "1" : "0",
                Guid guid           => $"'{guid}'",
                byte[] bytes        => $"CONVERT(VARBINARY(MAX), '0x{BytesToHex(bytes)}', 1)",
                _                   => value.ToString(),
            };
        }

        /// <summary>
        /// Converts an array of bytes to a long uppercase hex-String.
        /// </summary>
        private static string BytesToHex(IEnumerable<byte> bytes) => string.Concat(bytes.Select(x => x.ToString("X2")));
    }

}
