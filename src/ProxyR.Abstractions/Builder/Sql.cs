using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
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

            switch (parts.Length)
            {
                case 1:
                    return ("dbo", parts[0]);
                case 2:
                    return (parts[0], parts[1]);
                default:
                    throw new InvalidOperationException($"The given SQL identifier [{identifier}] cannot be split.");
            }

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
                switch (jValue.Type)
                {
                    case JTokenType.Bytes:
                        value = (byte[])jValue;
                        break;
                    case JTokenType.Integer:
                        value = int.Parse(jValue.ToString(CultureInfo.InvariantCulture));
                        break;
                    case JTokenType.Float:
                        value = float.Parse(jValue.ToString(CultureInfo.InvariantCulture));
                        break;
                    case JTokenType.Boolean:
                        value = bool.Parse(jValue.ToString(CultureInfo.InvariantCulture));
                        break;
                    case JTokenType.Null:
                    case JTokenType.Undefined:
                        value = null;
                        break;
                    case JTokenType.Date:
                        value = DateTime.Parse(jValue.ToString(CultureInfo.InvariantCulture));
                        break;
                    case JTokenType.String:
                    case JTokenType.TimeSpan:
                    case JTokenType.Guid:
                    case JTokenType.Uri:
                        value = jValue.ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown JTokenType [{value}].");
                }
            }

            if (value == null || value == DBNull.Value)
            {
                return "NULL";
            }

            switch (value)
            {
                case string stringValue:
                    return $"'{stringValue.Replace("'", "''")}'";
                case DateTime dateTime:
                    return $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";
                case bool isTrue:
                    return isTrue ? "1" : "0";
                case Guid guid:
                    return $"'{guid}'";
                case byte[] bytes:
                    return $"CONVERT(VARBINARY(MAX), '0x{BytesToHex(bytes)}', 1)";
                default:
                    return value.ToString();
            }
        }

        /// <summary>
        /// Converts an array of bytes to a long uppercase hex-String.
        /// </summary>
        private static string BytesToHex(IEnumerable<byte> bytes) => string.Concat(bytes.Select(x => x.ToString("X2")));
    }

}
