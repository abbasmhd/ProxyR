using ProxyR.Core.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ProxyR.Abstractions.Execution
{
    /// <summary>
    /// Contains a column-to-entity mapping, that can be stored once and repeatedly-used for speed.
    /// </summary>
    public class DbEntityMap
    {
        /// <summary>
        /// Holds each mapping for each property inside the entity.
        /// </summary>
        public IDictionary<string, DbEntityMapEntry> Entries { get; } = new Dictionary<string, DbEntityMapEntry>();

        /// <summary>
        /// The entity that this mapping describes.
        /// </summary>
        public Type EntityType { get; protected set; }

        /// <summary>
        /// Holds the mappings that make up the key.
        /// </summary>
        public IList<DbEntityMapEntry> Keys { get; } = new List<DbEntityMapEntry>();

        /// <summary>
        /// Whenever a mapping is created, it gets stored in the cache, referenced by the entity type.
        /// </summary>
        public static IDictionary<Type, DbEntityMap> Cache { get; } = new Dictionary<Type, DbEntityMap>();

        /// <summary>
        /// Create a new entity map.
        /// </summary>
        public static DbEntityMap Create<TEntity>()
        {
            var entityMap = Create(typeof(TEntity));
            return entityMap;
        }

        /// <summary>
        /// Gets or Creates a new entity map for a given entity type.
        /// </summary>
        public static DbEntityMap GetOrCreate<TEntity>()
        {
            var entityMap = GetOrCreate(typeof(TEntity));
            return entityMap;
        }

        /// <summary>
        /// Gets or Creates a new entity map for a given entity type.
        /// </summary>
        public static DbEntityMap GetOrCreate(Type entityType)
        {
            var entityMap = Cache.GetOrCreate(entityType, () => Create(entityType));
            return entityMap;
        }

        /// <summary>
        /// Create a new entity map.
        /// </summary>
        public static DbEntityMap Create(Type entityType)
        {
            var map = new DbEntityMap();

            var properties = entityType.GetProperties();
            foreach (var property in properties)
            {
                // Create the mapping entry for this property.
                var mapEntry = new DbEntityMapEntry
                {
                    Property = property,
                    ColumnName = property.Name
                };

                // Figure out the column name.
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null && !string.IsNullOrWhiteSpace(columnAttribute.Name))
                {
                    mapEntry.ColumnName = columnAttribute.Name;
                }

                // Does it have a key attribute? Signifying the primary key.
                var keyAttribute = property.GetCustomAttribute<KeyAttribute>();
                if (keyAttribute != null)
                {
                    mapEntry.IsKey = true;
                    map.Keys.Add(mapEntry);
                }

                // Set the setter action (using reflection, change to use expression-trees for faster results).
                mapEntry.ValueSetter = (entry, entity, value) => entry.Property.SetValue(entity, value);
                map.Entries[mapEntry.ColumnName] = mapEntry;

            }

            return map;
        }
    }
}
