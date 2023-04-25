using ProxyR.Core.Extensions;
using System;

namespace ProxyR.Abstractions.Utilities
{
    /// <summary>
    /// This class provides utility methods for dealing with null values.
    /// </summary>
    public class NullUtility
    {
        /// <summary>
        /// Checks if the given object is null.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <returns>True if the object is null, false otherwise.</returns>
        public static bool IsNull(object value) => UnwrapNullable(value) == null;

        /// <summary>
        /// Checks if the given object is null or an empty string.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <returns>True if the object is null or an empty string, false otherwise.</returns>
        public static bool IsNullOrEmpty(object value) => IsNull(value) || value is string stringValue && stringValue == String.Empty;

        /// <summary>
        /// Converts an empty value to null.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>Null if the value is empty, otherwise the original value.</returns>
        public static object EmptyToNull(object value) => IsNullOrEmpty(value) ? null : value;

        /// <summary>
        /// Converts a given value to DBNull if it is null or empty.
        /// </summary>
        public static object NullToDbNull(object value) => IsNullOrEmpty(value) ? DBNull.Value : value;


        /// <summary>
        /// Unwraps a nullable object and returns the underlying value.
        /// </summary>
        /// <param name="value">The object to unwrap.</param>
        /// <returns>The underlying value of the nullable object.</returns>
        public static object UnwrapNullable(object value)
        {
            if (value == null || value is DBNull)
            {
                return null;
            }
            var type = value.GetType();
            return !type.IsDefinedNullable() ? value : Convert.ChangeType(value, type.GetNullableUnderlyingType());
        }
    }
}
