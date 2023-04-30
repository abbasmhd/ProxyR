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

        /// <summary>
        /// Joins the given strings into a comma-separated list, surrounded by square brackets.
        /// </summary>
        /// <param name="names">The strings to join.</param>
        /// <returns>
        /// A comma-separated list of the given strings, surrounded by square brackets.
        /// </returns>
        public static string ColumnReferences(params string[] names) => String.Join(", ", names.Select(name => $"[{name}]"));

        /// <summary>
        /// Creates a string of column names in the format [name] separated by new lines.
        /// </summary>
        /// <param name="names">The names of the columns.</param>
        /// <returns>
        /// A string of column names in the format [name] separated by new lines.
        /// </returns>
        public static string ColumnLines(params string[] names) => String.Join("\n, ", names.Select(name => $"[{name}]"));

        /// <summary>
        /// Splits a string of parts separated by '.' into an array of strings, trimming any '[' and ']' characters.
        /// </summary>
        public static string[] SplitIdentifierParts(string chainOfParts) => chainOfParts.Split('.').Select(ids => ids.Trim('[', ']')).ToArray();

        /// <summary>
        /// Splits a SQL identifier into its schema and object name parts.
        /// </summary>
        /// <param name="identifier">The SQL identifier to split.</param>
        /// <returns>A tuple containing the schema and object name parts.</returns>
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

        /// <summary>
        /// Gets the property values of an object from a list of PropertyInfo objects.
        /// </summary>
        /// <param name="obj">The object to get the property values from.</param>
        /// <param name="properties">The list of PropertyInfo objects.</param>
        /// <returns>An array of objects containing the property values.</returns>
        public static object[] GetPropertyValues(object obj, IEnumerable<PropertyInfo> properties) => properties.Select(p => p.GetValue(obj)).ToArray();

        /// <summary>
        /// Joins the given strings with a new line and wraps each string in parentheses.
        /// </summary>
        /// <param name="contents">The strings to join.</param>
        /// <returns>
        /// A string containing the joined strings, each wrapped in parentheses.
        /// </returns>
        public static string ParenthesisLines(params string[] contents) => String.Join("\n, ", contents.Select(r => $"({r})"));

        /// <summary>
        /// Joins the given strings into a single string, separated by a new line and a comma.
        /// </summary>
        public static string CommaLines(params string[] lines) => String.Join("\n, ", lines);

        /// <summary>
        /// Generates a SQL column definition string for a given column name, type, nullability, default expression, column name padding, and collation.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="type">The data type of the column.</param>
        /// <param name="isNullable">Whether the column is nullable.</param>
        /// <param name="defaultExpression">The default expression for the column.</param>
        /// <param name="columnNamePadding">The padding for the column name.</param>
        /// <param name="doPadding">Whether to do padding.</param>
        /// <param name="collation">The collation for the column.</param>
        /// <returns>A SQL column definition string.</returns>
        public static string ColumnDefinition(string columnName, string type, bool? isNullable = null, string defaultExpression = null, int columnNamePadding = 0, bool doPadding = false, string collation = null)
        {
            type = type.ToUpper();

            var columnPart = $"[{columnName}]".PadRight(doPadding ? columnNamePadding : 0);

            var collationPart = String.Empty;

            if (type.IndexOf("CHAR", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                collationPart = $"COLLATE {(String.IsNullOrWhiteSpace(collation) ? "DATABASE_DEFAULT" : collation)}";
            }

            var nullablePart = String.Empty;

            if (isNullable != null)
            {
                nullablePart = isNullable == true ? "NULL" : "NOT NULL";
            }

            var defaultPart = String.Empty;

            if (defaultExpression != null)
            {
                defaultPart = String.IsNullOrWhiteSpace(defaultExpression)
                    ? $"= ''"
                    : $"= {defaultExpression}";
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

        /// <summary>
        /// Joins the given values into a comma-separated string, with each value quoted.
        /// </summary>
        public static string Values(IEnumerable<object> values) => String.Join(", ", values.SelectQuoted());

        /// <summary>
        /// Returns a string representation of the given values.
        /// </summary>
        /// <param name="values">The values to be converted to a string.</param>
        /// <returns>A string representation of the given values.</returns>
        public static string Values(params object[] values) => Values((IEnumerable<object>)values);

        /// <summary>
        /// Selects the quoted version of each value in the given sequence.
        /// </summary>
        /// <param name="values">The sequence of values to quote.</param>
        /// <returns>The sequence of quoted values.</returns>
        public static IEnumerable<string> SelectQuoted(this IEnumerable<object> values)
        {
            foreach (var value in values)
            {
                yield return Quote(value);
            }
        }

        /// <summary>
        /// Converts an object to a quoted string for use in a SQL query.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>A quoted string representation of the object.</returns>
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
                        value = DateTime.Parse(jValue.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                        break;
                    case JTokenType.String:
                    case JTokenType.TimeSpan:
                    case JTokenType.Guid:
                    case JTokenType.Uri:
                        value = jValue.ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        // Should never happen.
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
        /// Converts a sequence of bytes to a hexadecimal string.
        /// </summary>
        public static string BytesToHex(IEnumerable<byte> bytes) => string.Concat(bytes.Select(x => x.ToString("X2")));
    }

}
