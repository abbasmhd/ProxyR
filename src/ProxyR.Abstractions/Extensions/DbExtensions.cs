using System;
using System.Data;

namespace ProxyR.Abstractions.Extensions
{
    public static class DbExtensions
    {
        /// <summary>
        /// Determines whether the IDataRecord contains a column with the specified name.
        /// </summary>
        /// <param name="reader">The IDataRecord to check for the column.</param>
        /// <param name="columnName">The name of the column to search for.</param>
        /// <returns>true if the IDataRecord contains a column with the specified name; otherwise, false.</returns>
        public static bool HasColumn(this IDataRecord reader, string columnName)
        {
            for (var index = 0; index < reader.FieldCount; index++)
            {
                if (reader.GetName(index).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
