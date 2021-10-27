using System;
using System.Data;

namespace ProxyR.Abstractions.Extensions
{
    public static class DbExtensions
    {
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
