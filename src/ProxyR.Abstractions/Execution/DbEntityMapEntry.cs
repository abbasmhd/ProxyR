using ProxyR.Core.Extensions;
using System;
using System.ComponentModel;
using System.Reflection;

namespace ProxyR.Abstractions.Execution
{
    /// <summary>
    /// The delegate used in DbEntityMapEntry to define the code to call when setting the value of the entity's property.
    /// </summary>
    public delegate void SetValueDelegate(DbEntityMapEntry entry, object entity, object value);

    /// <summary>
    /// Provides a mapping between a column/field and a property in a type of entity.
    /// Parented by the DbEntityMap.
    /// </summary>
    public class DbEntityMapEntry
    {
        /// <summary>
        /// The parent entity-map that contains the whole type's mappings.
        /// </summary>
        public DbEntityMap EntityMap { get; set; }

        /// <summary>
        /// The column name to transform from or to.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// The entity's property to which values will be read and written.
        /// </summary>
        public PropertyInfo Property { get; set; }

        /// <summary>
        /// The action called to set the value of the entity property.
        /// </summary>
        public SetValueDelegate ValueSetter { get; set; }

        /// <summary>
        /// Gets whether or not the column represents part of the primary-key.
        /// </summary>
        public bool IsKey { get; set; }

        /// <summary>
        /// Set's a property within an entity to the value given.
        /// A conversion will be attempted, and DBNulls will be substituted for NULLs.
        /// </summary>
        public void SetValue(object entity, object value)
        {
            if (value is DBNull)
            {
                value = null;
            }

            // Attempt conversion.
            if (value != null)
            {
                value = Convert(value, Property.PropertyType);
            }

            // Call the bespoke set-value action.
            ValueSetter(this, entity, value);
        }

        /// <summary>
        /// Converts the primitive value to the desired type, accounting for nullable types.
        /// </summary>
        private static object Convert(object value, Type targetType)
        {
            // Simply pass through nulls.
            if (value == null)
            {
                return null;
            }

            // Use a type-converter for most conversions from a String.
            // This works well for types such as a Guid.
            if (value is string stringValue && targetType != typeof(object) && targetType != typeof(DateTime))
            {
                var converter = TypeDescriptor.GetConverter(targetType);
                var convertedValue = converter.ConvertFromInvariantString(stringValue);
                return convertedValue;
            }

            // Get the non-nullable version of the type, and convert into that.
            var underlyingType = targetType.GetNullableUnderlyingType();
            var changedValue = System.Convert.ChangeType(value, underlyingType);
            return changedValue;
        }
    }
}
