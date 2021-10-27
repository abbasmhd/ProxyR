using ProxyR.Core.Extensions;
using System;

namespace ProxyR.Abstractions.Utilities
{
    public class NullUtility
    {
        /// <summary>
        /// Checks if the value is NULL, even if it's a Nullable<> object.
        /// </summary>
        public static bool IsNull(object value) => UnwrapNullable(value) == null;

        /// <summary>
        /// Checks if the object is NULL, or an empty-string?
        /// </summary>
        public static bool IsNullOrEmpty(object value) => IsNull(value) || value is string stringValue && stringValue == String.Empty;

        /// <summary>
        /// Converts an empty-string to a NULL, if value is an empty-String.
        /// </summary>
        public static object EmptyToNull(object value) => IsNullOrEmpty(value) ? null : value;

        /// <summary>
        /// Should the value be NULL or DBNull, this will return it as DBNull.
        /// </summary>
        public static object NullToDbNull(object value) => IsNullOrEmpty(value) ? DBNull.Value : value;

        /// <summary>
        /// If the object is of type Nullable<T>, this will unwrap into a non-nullable version.
        /// </summary>
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
