using ProxyR.Abstractions.Utilities;
using ProxyR.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyR.Abstractions.Extensions
{
    public static class DataExtensions
    {
        /// <summary>
        /// Adds field values to a row, based on the properties inside an entity object.
        /// </summary>
        public static void SetValues<TEntity>(this DataRow row, TEntity item)
        {
            // Get a list of all properties for that type.
            var properties = typeof(TEntity)
                .GetProperties()
                .Where(p => p.PropertyType.IsPrimitive());

            // Set each field on the table from 
            // the corresponding entity property.
            foreach (var property in properties)
            {
                var value = property.GetValue(item);
                value = NullUtility.UnwrapNullable(value);
                value = NullUtility.NullToDbNull(value);
                row[property.Name] = value;
            }
        }
    }
}
